using System;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Vec2f = OpenTK.Vector2;
using Vec3f = OpenTK.Vector3;
using Vec4f = OpenTK.Vector4;
using Mat3f = OpenTK.Matrix3;
using Mat4f = OpenTK.Matrix4;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace PirateCraft
{
    //A buffer on GPU, vertex buffer, index buffer .. 
    public class GPUBuffer
    {
        private int _glBufferId = 0;
        private int _itemCount = 0;
        private int _itemSize = 0;

        public BufferTarget BufferTarget { get; private set; } = BufferTarget.ArrayBuffer;
        public int ItemCount { get { return _itemCount; } }
        public int ItemSize { get { return _itemSize; } }

        public GPUBuffer(BufferTarget t, GpuDataArray items)
        {
            BufferTarget = t;
            _glBufferId = GL.GenBuffer();
            Allocate(items);
        }
        public void Bind()
        {
            GL.BindBuffer(BufferTarget, _glBufferId);
        }
        public void Unbind()
        {
            GL.BindBuffer(BufferTarget, 0);
        }
        public GpuDataArray CopyDataFromGPU(int itemOffset = 0, int itemCount = -1)
        {
            //Copies GPU data into a temporary byte array.
            //GpuDataArray is a kind of proxy class that munges data into a managed byte array.

            int offsetBytes = itemOffset * _itemSize;
            int lengthBytes = (itemCount <= -1) ? (_itemCount * _itemSize) : ((int)itemCount * _itemSize);
            Bind();
            IntPtr pt = GL.MapBufferRange(BufferTarget, (IntPtr)offsetBytes, (IntPtr)lengthBytes, BufferAccessMask.MapReadBit);
            if (pt == IntPtr.Zero)
            {
                Gu.BRThrowException("Failed to map OpenGL Buffer.");
            }
            byte[] managedArray = new byte[lengthBytes];
            Marshal.Copy(pt, managedArray, 0, (int)lengthBytes);
            GL.UnmapBuffer(BufferTarget);
            Unbind();

            GpuDataArray d = new GpuDataArray(_itemSize, _itemCount, managedArray);
            return d;
        }
        ~GPUBuffer()
        {
            if (GL.IsBuffer(_glBufferId))
            {
                GL.DeleteBuffers(1, ref _glBufferId);
            }
        }
        void Allocate(GpuDataArray items)
        {
            _itemCount = items.Count;
            _itemSize = items.ItemSizeBytes;
           // var pinnedArray = GCHandle.Alloc(items.Bytes, GCHandleType.Pinned);
            Bind();
            GL.BufferData(
                      BufferTarget,
                (int)(items.Count * items.ItemSizeBytes),
                items.AllocPtr(),
                BufferUsageHint.StaticDraw
                );
            items.FreePtr();
          // pinnedArray.Free();
            Unbind();

        }
    }

}