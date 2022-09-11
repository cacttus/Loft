using System.ComponentModel;
using System.Data;
using System.Runtime.InteropServices;
using System.Text;
using OpenTK.Graphics.OpenGL4;
using System.Reflection;

namespace PirateCraft
{
  public enum LanguageCode
  {
    //just copy this, use the descriptors.
    //https://saimana.com/list-of-country-locale-code/
    NA,
    ar,
    ar_DZ,
    ar_BH,
    ar_EG,
    ar_IQ,
    ar_JO,
    ar_KW,
    ar_LB,
    ar_LY,
    ar_MA,
    ar_OM,
    ar_QA,
    ar_SA,
    ar_SD,
    ar_SY,
    ar_TN,
    ar_AE,
    ar_YE,
    de_DE,
    en,
    en_AU,
    en_CA,
    en_IN,
    en_IE,
    en_MT,
    en_NZ,
    en_PH,
    en_SG,
    en_ZA,
    en_GB,
    en_US,
    es,
    es_AR,
    es_BO,
    es_CL,
    es_CO,
    es_CR,
    es_DO,
    es_EC,
    es_SV,
    es_GT,
    es_HN,
    es_MX,
    es_NI,
    es_PA,
    es_PY,
    es_PE,
    es_PR,
    es_ES,
    es_US,
    es_UY,
    es_VE,
    nl,
    nl_BE,
    nl_NL,
    pt,
    pt_PT,
    pt_BR,
    ru,
    ru_RU,
    uk,
    uk_UA,
    ja,
    ja_JP,
    zh,
    zh_CN,
    zh_HK,
    zh_SG,
    zh_TW,
  }
  public class InlineTrans : Attribute
  {
    public Dictionary<LanguageCode, string> InlineTranslations = null;
    // This is the attribute constructor.
    public InlineTrans(string[] trans)
    {
      Gu.Assert(trans.Length % 2 == 0);
      InlineTranslations = InlineTranslations.ConstructIfNeeded();
      for (int i = 0; i < trans.Length; i += 2)
      {
        try
        {
          LanguageCode c = (LanguageCode)Enum.Parse(typeof(LanguageCode), trans[i + 0]);
          InlineTranslations.Add(c, trans[i + 1]);
        }
        catch (Exception ex)
        {
          Gu.Log.Error($"Failed to parse inline translation {trans[i + 0]}{trans[i + 1]}");
        }
      }
    }
  }
  public enum Phrase
  {
    //The name of this enum must match the name in the Phrases.csv file.
    None,
    Foob,
    LongText,
    ImagesFolder,
    Reload,
    AddImage,
    EditImage,
    CreateProject,
    EditProject,
    Name,
    Image,
    Atlas,
    TileSize,
    Spacing,
    Margin,
    AtlasParameters,
    ProjectPath,
    ImageLocation,
    ImageAlreadyExists,
    AddItem,
    EditItem,
    RemoveItem,
    CopyItem,
    RefreshForm,
    Warning,
    RecentFiles,
    Ok,
    Cancel,
    File,
    Exit,
    Language,
    FolderDoesNotExist,
    FileDoesNotExist,
    Options,
    View,
    Sprites,
    Objects,
    Layers,
    Undo,
    Redo,
    Edit,
    Tools,
    Theme,
    Light,
    Dark,
    English,
    Spanish,
    Error,
    Info,
    New,
    Open,
    OpenProject,
    CloseProject,
    Save,
    SaveAs,
    AddEdit,
    NewProject,
    ProjectName,
    SelectFolder,
    ProjectNameIsEmpty,
    ProjectFolderExists,
    ConfirmOverwrite,
    About,
    Help,
    PleaseCorrectTheFollowingErrors,
    PathHasInvalidChars,
    Close,
    FormatCode,
    Unsaved,
    SaveProject,
    SaveProjectAs,
    SaveFile,
    SaveFileAs,
    LoadLastProjectFile,
    [InlineTrans(new string[] { "en", "Debug Information", "es", "Información de Depuración", "ru", "Отладочная информация" })] DebugInfoHeader,
    [InlineTrans(new string[] { "en", "Hover Object To See Information ", "es", "Pase El Cursor Sobre El Objeto Para Ver La Información", "ru", "Наведите Указатель Мыши На Объект, Чтобы Увидеть Информацию" })] DebugInfoMustSelect,
  }
  public class Translator
  {
    public LanguageCode LanguageCode { get; set; } = LanguageCode.es;
    private BoJankEnumDataTable<LanguageCode, Phrase, String> _translateDataTable = null;
    public Translator()
    {
      //No, we don't adhere to any CSV standard.
      try
      {
        FileLoc f = new FileLoc("phrases.csv", FileStorage.Embedded);
        var csvData = new BoJankEnumCSV<LanguageCode, Phrase, String>(f, (x) => { return x; });

        Gu.Log.Debug(csvData.DebugPrintTable().ToString());
        _translateDataTable = csvData.DataTable;
      }
      catch (Exception ex)
      {
        Gu.Log.Error(ex.ToString());
        Gu.DebugBreak();
      }
    }
    public string Translate(Phrase p)
    {
      string? dat = "";
      bool found = false;
      if (_translateDataTable == null)
      {
        dat = GetDefaultUntranslated(p);
      }
      else
      {
        //Try given language.
        dat = _translateDataTable.Cell(LanguageCode, p, false);
        if (dat == null)
        {
          //Try the "custom attribute"
          dat = GetInlineTrans(LanguageCode, p);
          if (dat == null)
          {
            //Default to english  if avail
            dat = _translateDataTable.Cell(LanguageCode.en, p, false);
            if (dat == null)
            {
              //No data, us enum name and print a big warning.
              dat = GetDefaultUntranslated(p);
            }
          }
          else
          {
            found = true;
          }
        }
        else
        {
          found = true;
        }
      }
      if (found == false)
      {
        //Since it wasn't found, add not found asterisks.
        dat = $"**{dat}";
      }

      return dat;
    }
    private String GetDefaultUntranslated(Phrase p)
    {
      return $"*{p.ToString()}*";
    }
    private string? GetInlineTrans(LanguageCode c, Phrase p)
    {
      InlineTrans att = p.GetAttribute<InlineTrans>();
      if (att != null)
      {
        att.InlineTranslations.TryGetValue(c, out var s);
        return s;
      }
      return null;
    }

  }
}