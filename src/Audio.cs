using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Audio.OpenAL;

//Translation: This is full of bugs. This needs to be fixed.
namespace PirateCraft
{
  public class AudioManager : IDisposable
  {
    private bool _kill = false;
    private ALDevice _device;
    private ALContext _context;
    private List<AudioStream> _streams = new List<AudioStream>();
    private Thread _thread;
    public AudioManager()
    {
      _thread = new Thread(() =>
      {
        _device = ALC.OpenDevice("");
        _context = ALC.CreateContext(_device, new int[] { });
        if (_context == null)
        {
          Gu.Log.Error("Failed to create OpenAL device");
          ALC.CloseDevice(_device);
          return;
        }
        ALC.MakeContextCurrent(_context);
        CheckALErrors();
        AL.RegisterOpenALResolver();
        CheckALErrors();

        while (true)
        {
          List<AudioStream> copy;
          lock (_streams)
          {
            copy = new List<AudioStream>(_streams);
          }
          foreach (AudioStream a in copy)
          {
            a.UpdateAsync();
            if (a.State == AudioStream.PlayState.StreamEnded)
            {
              lock (_streams)
              {
                _streams.Remove(a);
              }
            }
          }
          if (_kill)
          {
            break;
          }
          Thread.Sleep(10);
        }

        _streams.Clear();

        ALC.MakeContextCurrent(ALContext.Null);
        CheckALErrors();
        ALC.CloseDevice(_device);
        CheckALErrors();
      });
      _thread.IsBackground = true;
      _thread.Priority = ThreadPriority.BelowNormal;
      _thread.Start();

    }
    public static void CheckALErrors()
    {
      for (int i = 0; Gu.WhileTrueGuard(i, Gu.c_intMaxWhileTrueLoop); ++i)
      {
        //NOTE: you can only call getError when there is an AL context.
        ALError e = AL.GetError();
        if (e != ALError.NoError)
        {
          string e_str = AL.GetErrorString(e);
          Gu.Log.Error("OpenAL Error: " + e + " => " + e_str);
        }
        else
        {
          break;
        }
      }

    }
    ~AudioManager()
    {
      _kill = true;
      _thread.Join();
      Dispose();
    }
    public void Dispose()
    {
      GC.SuppressFinalize(this);
    }
    public AudioStream Play(FileLoc loc)
    {
      return Play(loc, new vec3(0, 0, 0));
    }
    public AudioStream Play(FileLoc loc, vec3 position)
    {
      if (_streams.Count > 1000)
      {
        Gu.Log.WarnCycle("Too many audio streams > 1000 audio streams detected.");
        return null;
      }
      AudioStream stream = new AudioStream(loc, position);
      lock (_streams)
      {
        _streams.Add(stream);
      }
      stream.Play();
      return stream;
    }
  }
  public class AudioStream
  {
    public enum PlayState
    {
      None,
      Playing,
      Paused,
      Stopped,
      StreamEnded
    }
    private const int BufferCount = 16;
    private int[] _buffers = null;
    private int _sourceId = 0;
    private VorbisStream _vorbisStream = null;
    private int _iCurrentBuffer = 0;
    private FileLoc _fileLoc = null;
    private vec3 _pos = new vec3(0, 0, 0);
    private List<PlayState> _commands = new List<PlayState>();
    private bool _loop = false;
    public bool Loop { get { return _loop; } set { _loop = value; } }
    public vec3 Position
    {
      get { return _pos; }
      set
      {
        _pos = value;
        if (_sourceId != 0)
        {
          var p = _pos.ToOpenTK();
          AL.Source(_sourceId, ALSource3f.Position, ref p);
          AudioManager.CheckALErrors();
        }
      }
    }
    public PlayState State { get; private set; } = PlayState.Stopped;

    public AudioStream(FileLoc loc, vec3 pos)
    {
      //NOTE* the constructor here is called synchronously.
      Gu.Assert(loc != null);
      _fileLoc = loc;
      _pos = pos;
      //NOTE* the constructor here is called synchronously.
    }
    public void Play()
    {
      lock (_commands)
      {
        _commands.Add(PlayState.Playing);
      }
    }
    public void Pause()
    {
      lock (_commands)
      {
        _commands.Add(PlayState.Paused);
      }
    }
    public void Stop()
    {
      lock (_commands)
      {
        _commands.Add(PlayState.Stopped);
      }
    }
    private void PlayAsync()
    {
      if (State != PlayState.Playing)
      {
        InitBuffers();
      }
      State = PlayState.Playing;
      AL.SourcePlay(_sourceId);
      AudioManager.CheckALErrors();
    }
    private void PauseAsync()
    {
      State = PlayState.Paused;
      AL.SourcePause(_sourceId);
      AudioManager.CheckALErrors();
    }
    private void StopAsync()
    {
      _vorbisStream.Reset(); //Position=0
      _iCurrentBuffer = 0;
      State = PlayState.Stopped;
      AL.SourceStop(_sourceId);
      AudioManager.CheckALErrors();
    }
    private void InitBuffers()
    {
      if (_sourceId == 0)
      {
        _sourceId = AL.GenSource();
        AudioManager.CheckALErrors();
        Position = _pos;

        AL.Source(_sourceId, ALSourceb.Looping, false);
        AudioManager.CheckALErrors();
      }

      if (_buffers == null)
      {
        _buffers = AL.GenBuffers(BufferCount);
        AudioManager.CheckALErrors();
      }

      AL.SourceStop(_sourceId);
      AudioManager.CheckALErrors();

      int queued = 0;
      AL.GetSource(_sourceId, ALGetSourcei.BuffersQueued, out queued);
      AudioManager.CheckALErrors();
      for (int i = 0; i < queued; i++)
      {
        AL.SourceUnqueueBuffer(_sourceId);
      }
      AudioManager.CheckALErrors();
      for (int i = 0; i < _buffers.Length; ++i)
      {
        FillNextBuffer();
      }

    }
    private void FillNextBuffer()
    {
      if (State == PlayState.StreamEnded)
      {
        return;
      }

      //Get our stream
      if (_vorbisStream == null)
      {
        _vorbisStream = new VorbisStream(_fileLoc);
      }
      var data = _vorbisStream.GetData();

      //Check for end of stream, or loop
      if (data == null)
      {
        if (Loop)
        {
          _vorbisStream = new VorbisStream(this._fileLoc);
          data = _vorbisStream.GetData();
          if (data == null)
          {
            //This is an error _ i don't know why vorbis is not decoding after we reset the stream
            Gu.Log.Error("Stb vorbis stopped decoding a valid file ");
            State = PlayState.StreamEnded;
            return;
          }
        }
        else
        {
          _vorbisStream = null;
          State = PlayState.StreamEnded;
          return;
        }
      }

      //Note: data buffer must be the same size as SampleRate
      AL.BufferData(_buffers[_iCurrentBuffer], ALFormat.Stereo16, data, _vorbisStream.SampleRate);
      AudioManager.CheckALErrors();
      AL.SourceQueueBuffer(_sourceId, _buffers[_iCurrentBuffer]);
      AudioManager.CheckALErrors();

      _iCurrentBuffer = (_iCurrentBuffer + 1) % _buffers.Length;
    }
    private void DoCommandsAsync()
    {
      List<PlayState> cmdCopy;
      lock (_commands)
      {
        cmdCopy = new List<PlayState>(_commands);
        _commands.Clear();
      }
      foreach (var cmd in cmdCopy)
      {
        if (cmd == PlayState.Stopped)
        {
          StopAsync();
        }
        else if (cmd == PlayState.Paused)
        {
          PauseAsync();
        }
        else if (cmd == PlayState.Playing)
        {
          PlayAsync();
        }
      }
    }
    public void UpdateAsync()
    {
      DoCommandsAsync();

      int nproc = 0;
      AL.GetSource(_sourceId, ALGetSourcei.BuffersProcessed, out nproc);
      AudioManager.CheckALErrors();

      if (nproc > 0)
      {

        AL.SourceUnqueueBuffer(_sourceId);
        AudioManager.CheckALErrors();

        for (int iproc = 0; iproc < nproc; iproc++)
        {
          FillNextBuffer();
        }
      }
    }

  }
  public class InMemorySongData
  {
    public byte[] Bytes;
  }
  public class VorbisStream
  {
    static Dictionary<FileLoc, InMemorySongData> _cache = new Dictionary<FileLoc, InMemorySongData>(new FileLoc.EqualityComparer());
    //If the vorbis file is sufficiently small we can just load it  into memory. 

    //private Stream _stream = null;
    private StbVorbisSharp.Vorbis _vorbis = null;
    private int _read = 0;

    int _position = 0;//Note: position is the decoded * the channels.
    public int Position { get { return _position; } private set { _position = value; } }//TODO: set the position manual if need
    public int SampleRate => _vorbis.SampleRate;
    public int Channels => _vorbis.Channels;
    public FileLoc _file;

    //stbvorbis needs its own  copy of the data.
    //Sucks for in memory files, however, if we are going to s
    //et up streaming, we don't need this - just pass a stream to vorbis.
    public byte[] _data_copy = null;

    public VorbisStream(FileLoc file)
    {
      Gu.Assert(file != null);
      _file = file;
      LoadIntoMemory();
    }
    private byte[] LoadIntoMemory()
    {
      if (_data_copy == null)
      {
        InMemorySongData dat = null;
        if (!_cache.TryGetValue(_file, out dat))
        {
          dat = new InMemorySongData();
          dat.Bytes = _file.GetBytes();
          _cache.Add(_file, dat);
        }
        Gu.Assert(dat != null);
        _data_copy = new byte[dat.Bytes.Length];
        Buffer.BlockCopy(dat.Bytes, 0, _data_copy, 0, dat.Bytes.Length);
      }

      return _data_copy;
    }
    public void Reset()
    {
      _vorbis.Restart();
      _position = 0;
    }
    private short[] LoadAndDecode()
    {
      var memory = LoadIntoMemory();
      if (_vorbis == null)
      {
        _vorbis = StbVorbisSharp.Vorbis.FromMemory(memory);
      }

      //This is Stb's decode. SubmitBuffer..?
      _vorbis.SubmitBuffer();

      return _vorbis.SongBuffer;
    }
    public short[] GetData()
    {
      var son = LoadAndDecode();

      if (_vorbis.Decoded == 0)
      {
        //End of stream - note - that we're actually using one huge file here. there is no "file" streaming here.
        // we can file stream if we wish.. but for now .. just load the into memory

        return null;
      }
      else
      {
        short[] songbuffer_copy = new short[_vorbis.Decoded * _vorbis.Channels]; //If this is a byte[] then  *2
        Buffer.BlockCopy(son, 0, songbuffer_copy, 0, _vorbis.Decoded * _vorbis.Channels * 2);

        _position += _vorbis.Decoded;
        return songbuffer_copy;
      }
      return null;
    }

  }



}
