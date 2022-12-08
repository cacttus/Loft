using System.ComponentModel;
using System.Data;
using System.Runtime.InteropServices;
using System.Text;
using OpenTK.Graphics.OpenGL4;
using System.Reflection;
using System.Globalization;
using System.Text.Unicode;

namespace Loft
{
  #region Enums
  public enum RegionCode
  {
    //https://saimana.com/list-of-country-locale-code/
    NA
    , @sq_AL //  Albanian (Albania)
    , @sq// Albanian	
    , @ar_DZ // Arabic (Algeria)
    , @ar_BH // Arabic (Bahrain)
    , @ar_EG // Arabic (Egypt)
    , @ar_IQ // Arabic (Iraq)
    , @ar_JO // Arabic (Jordan)
    , @ar_KW // Arabic (Kuwait)
    , @ar_LB // Arabic (Lebanon)
    , @ar_LY // Arabic (Libya)
    , @ar_MA // Arabic (Morocco)
    , @ar_OM // Arabic (Oman)
    , @ar_QA // Arabic (Qatar)
    , @ar_SA // Arabic (Saudi Arabia)
    , @ar_SD // Arabic (Sudan)
    , @ar_SY // Arabic (Syria)
    , @ar_TN // Arabic (Tunisia)
    , @ar_AE // Arabic (United Arab Emirates)
    , @ar_YE // Arabic (Yemen)
    , @ar // Arabic
    , @be_BY // Belarusian (Belarus)
    , @be // Belarusian
    , @bn_IN // Bengali (India)
    , @bn_BD // Bengali (Bangladesh)
    , @bn // Bengali
    , @bg_BG // Bulgarian (Bulgaria)
    , @bg // Bulgarian
    , @ca_ES // Catalan (Spain)
    , @ca // Catalan
    , @zh_CN // Chinese (China)
    , @zh_HK // Chinese (Hong Kong)
    , @zh_SG // Chinese (Singapore)
    , @zh_TW // Chinese (Taiwan)
    , @zh // Chinese
    , @hr_HR // Croatian (Croatia)
    , @hr // Croatian
    , @cs_CZ // Czech (Czech Republic)
    , @cs // Czech
    , @da_DK // Danish (Denmark)
    , @da // Danish
    , @nl_BE // Dutch (Belgium)
    , @nl_NL // Dutch (Netherlands)
    , @nl // Dutch
    , @en_AU // English (Australia)
    , @en_CA // English (Canada)
    , @en_IN // English (India)
    , @en_IE // English (Ireland)
    , @en_MT // English (Malta)
    , @en_NZ // English (New Zealand)
    , @en_PH // English (Philippines)
    , @en_SG // English (Singapore)
    , @en_ZA // English (South Africa)
    , @en_GB // English (United Kingdom)
    , @en_US // English (United States)
    , @en // English
    , @et_EE // Estonian (Estonia)
    , @et // Estonian
    , @fi_FI // Finnish (Finland)
    , @fi // Finnish
    , @fr_BE // French (Belgium)
    , @fr_CA // French (Canada)
    , @fr_FR // French (France)
    , @fr_LU // French (Luxembourg)
    , @fr_CH // French (Switzerland)
    , @fr // French
    , @de_AT // German (Austria)
    , @de_DE // German (Germany)
    , @de_LU // German (Luxembourg)
    , @de_CH // German (Switzerland)
    , @de // German
    , @el_CY // Greek (Cyprus)
    , @el_GR // Greek (Greece)
    , @el // Greek
    , @iw_IL // Hebrew (Israel)
    , @iw // Hebrew
    , @hi_IN // Hindi (India)
    , @hu_HU // Hungarian (Hungary)
    , @hu // Hungarian
    , @is_IS // Icelandic (Iceland)
    , @is // Icelandic
    , @in_ID // Indonesian (Indonesia)
    , @in // Indonesian
    , @ga_IE // Irish (Ireland)
    , @ga // Irish
    , @it_IT // Italian (Italy)
    , @it_CH // Italian (Switzerland)
    , @it // Italian
    , @ja_JP // Japanese (Japan)
    , @ja_JP_JP // Japanese (Japan,JP)	
    , @ja // Japanese
    , @ko_KR // Korean (South Korea)
    , @ko // Korean
    , @lv_LV // Latvian (Latvia)
    , @lv // Latvian
    , @lt_LT // Lithuanian (Lithuania)
    , @lt // Lithuanian
    , @mk_MK // Macedonian (Macedonia)
    , @mk // Macedonian
    , @ms_MY // Malay (Malaysia)
    , @ms // Malay
    , @mt_MT // Maltese (Malta)
    , @mt // Maltese
    , @no_NO // Norwegian (Norway)
    , @no_NO_NY // Norwegian (Norway,Nynorsk)	
    , @no // Norwegian
    , @pl_PL // Polish (Poland)
    , @pl // Polish
    , @pt_BR // Portuguese (Brazil)
    , @pt_PT // Portuguese (Portugal)
    , @pt // Portuguese
    , @ro_RO // Romanian (Romania)
    , @ro // Romanian
    , @ru_RU // Russian (Russia)
    , @ru // Russian
    , @sr_BA // Serbian (Bosnia and Herzegovina)
    , @sr_ME // Serbian (Montenegro)
    , @sr_CS // Serbian (Serbia and Montenegro)
    , @sr_RS // Serbian (Serbia)
    , @sr // Serbian
    , @sk_SK // Slovak (Slovakia)
    , @sk // Slovak
    , @sl_SI // Slovenian (Slovenia)
    , @sl // Slovenian
    , @es_AR // Spanish (Argentina)
    , @es_BO // Spanish (Bolivia)
    , @es_CL // Spanish (Chile)
    , @es_CO // Spanish (Colombia)
    , @es_CR // Spanish (Costa Rica)
    , @es_DO // Spanish (Dominican Republic)
    , @es_EC // Spanish (Ecuador)
    , @es_SV // Spanish (El Salvador)
    , @es_GT // Spanish (Guatemala)
    , @es_HN // Spanish (Honduras)
    , @es_MX // Spanish (Mexico)
    , @es_NI // Spanish (Nicaragua)
    , @es_PA // Spanish (Panama)
    , @es_PY // Spanish (Paraguay)
    , @es_PE // Spanish (Peru)
    , @es_PR // Spanish (Puerto Rico)
    , @es_ES // Spanish (Spain)
    , @es_US // Spanish (United States)
    , @es_UY // Spanish (Uruguay)
    , @es_VE // Spanish (Venezuela)
    , @es // Spanish
    , @sv_SE // Swedish (Sweden)
    , @sv // Swedish
    , @th_TH // Thai (Thailand)
    , @th_TH_TH // Thai (Thailand,TH)	
    , @th // Thai
    , @tr_TR // Turkish (Turkey)
    , @tr // Turkish
    , @uk_UA // Ukrainian (Ukraine)
    , @uk // Ukrainian
    , @vi_VN // Vietnamese (Vietnam)
    , @vi // Vietnamese
  }
  public enum LanguageCode
  {
    //https://saimana.com/list-of-country-locale-code/
    NA
  , @sq// Albanian	
  , @ar // Arabic
  , @be // Belarusian
  , @bn // Bengali
  , @bg // Bulgarian
  , @ca // Catalan
  , @zh // Chinese
  , @hr // Croatian
  , @cs // Czech
  , @da // Danish
  , @nl // Dutch
  , @en // English
  , @et // Estonian
  , @fi // Finnish
  , @fr // French
  , @de // German
  , @el // Greek
  , @iw // Hebrew
  , @hu // Hungarian
  , @is // Icelandic
  , @in // Indonesian
  , @ga // Irish
  , @it // Italian
  , @ja // Japanese
  , @ko // Korean
  , @lv // Latvian
  , @lt // Lithuanian
  , @mk // Macedonian
  , @ms // Malay
  , @mt // Maltese
  , @no // Norwegian
  , @pl // Polish
  , @pt // Portuguese
  , @ro // Romanian
  , @ru // Russian
  , @sr // Serbian
  , @sk // Slovak
  , @sl // Slovenian
  , @es // Spanish
  , @sv // Swedish
  , @th // Thai
  , @tr // Turkish
  , @uk // Ukrainian
  , @vi // Vietnamese
  }
  public enum Phrase
  {
    //The name of this enum must match the name in the Phrases.csv file.
    None,//Empty, nothing, do not translate
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
    Version,
    Help,
    Settings,
    Test1,
    Test2,
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
    [InlineTranslation(new string[] { "en", "Debug Information", "es", "Información de Depuración", "ru", "Отладочная информация" })] DebugInfoHeader,
    [InlineTranslation(new string[] { "en", "Hover Object To See Information ", "es", "Pase El Cursor Sobre El Objeto Para Ver La Información", "ru", "Наведите Указатель Мыши На Объект, Чтобы Увидеть Информацию" })] DebugInfoMustSelect,
    Information,
  }
  #endregion

  public class InlineTranslation : Attribute
  {
    //Attribute for inline translations (without  file / database)
    public Dictionary<LanguageCode, string> InlineTranslations = null;
    public InlineTranslation(string[] trans)
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
  public class FontRangeUTF8
  {
    //Not using System.Text.Unicode.UnicodeRange being, possibly we just want subsets of most ranges (for example CJKU)
    //Character ranges correspond to the chars we 'try' to fit on a single texture.
    //Han, for instance is massive, so we would split it among CharacterRanges. 
    //Also, most of UTF8 is pointless characters.

    //Unicode Page
    public LanguageCode LanguageCode = LanguageCode.en;

    public int FirstChar { get; private set; } = UnicodeRanges.BasicLatin.FirstCodePoint;
    public int CharCount { get; private set; } = UnicodeRanges.BasicLatin.Length;

    public List<MtFontPatchInfo> FontPatchInfos = new List<MtFontPatchInfo>();//Mipmaps

    public FontRangeUTF8(UnicodeRange r, LanguageCode c)
    {
      LanguageCode = c;
      FirstChar = r.FirstCodePoint;
      CharCount = r.Length;
    }

  }

  public class GameTextCulture
  {
    public const LanguageCode FallbackLanguageCode = LanguageCode.en;
    private static Dictionary<LanguageCode, List<FontRangeUTF8>> LangRanges = null;

    public static LanguageCode? CultureInfoToLanguageCode(CultureInfo ci)
    {
      LanguageCode? ret = null;
      var name = ci.TwoLetterISOLanguageName;
      var e = Enum.Parse(typeof(LanguageCode), name);
      if (e != null)
      {
        ret = (LanguageCode)e;
      }
      return ret;
    }
    public static List<FontRangeUTF8> GetUnicodeRangesForLanguageCodeSorted(LanguageCode c)
    {
      if (LangRanges == null)
      {
        LangRanges = LangRanges.ConstructIfNeeded();
        var AddRange = (LanguageCode ccc, UnicodeRange ur, Dictionary<LanguageCode, List<FontRangeUTF8>> lang_ranges) =>
        {
          List<FontRangeUTF8>? ranges = null;
          if (!lang_ranges.TryGetValue(ccc, out ranges))
          {
            ranges = new List<FontRangeUTF8>();
            lang_ranges.Add(c, ranges);
          }
          ranges.Add(new FontRangeUTF8(ur, ccc)); //TODO: custom unicode ranges.
        };

        //Ugh..
        AddRange(LanguageCode.en, UnicodeRanges.BasicLatin, LangRanges);
        AddRange(LanguageCode.es, UnicodeRanges.BasicLatin, LangRanges);
        AddRange(LanguageCode.el, UnicodeRanges.BasicLatin, LangRanges);
        AddRange(LanguageCode.pt, UnicodeRanges.BasicLatin, LangRanges);
        AddRange(LanguageCode.ar, UnicodeRanges.Arabic, LangRanges);
        AddRange(LanguageCode.el, UnicodeRanges.GreekandCoptic, LangRanges);
        AddRange(LanguageCode.ru, UnicodeRanges.Cyrillic, LangRanges);
        AddRange(LanguageCode.bg, UnicodeRanges.Cyrillic, LangRanges);
        AddRange(LanguageCode.uk, UnicodeRanges.Cyrillic, LangRanges);
        AddRange(LanguageCode.zh, UnicodeRanges.CjkUnifiedIdeographs, LangRanges);
        AddRange(LanguageCode.ja, UnicodeRanges.CjkUnifiedIdeographs, LangRanges);
        AddRange(LanguageCode.ko, UnicodeRanges.CjkUnifiedIdeographs, LangRanges);
      }

      List<FontRangeUTF8>? ranges = null;

      if (LangRanges.TryGetValue(c, out ranges))
      {
        return ranges;
      }
      else
      {
        ranges = new List<FontRangeUTF8>();
        ranges.Add(new FontRangeUTF8(UnicodeRanges.BasicLatin, LanguageCode.en));
        Gu.DebugBreak();
      }

      ranges.Sort((x, y) => x.FirstChar - y.FirstChar);

      return ranges;
    }
    public static LanguageCode GetDefaultLanguage()
    {
      var ci = CultureInfo.CurrentCulture;
      var lc = CultureInfoToLanguageCode(ci);
      if (lc == null)
      {
        Gu.Log.Info($"Could not parse system culture '{ci.ToString()}'");
        lc = GameTextCulture.FallbackLanguageCode;
      }
      Gu.Assert(lc != null);
      return lc.Value;

    }
    public List<LanguageCode> GetInstalledLanguages()
    {
      List<LanguageCode> ret = new List<LanguageCode>();
      // Cultures that are associated with a language but are not specific to a country/region.
      var cultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures);//
      foreach (var ci in cultures)
      {
        var lc = CultureInfoToLanguageCode(ci);
        if (lc != null)
        {
          ret.Add(lc.Value);
        }
        else
        {
          Gu.Log.Info($"Could not parse system culture '{ci.ToString()}'");
        }
      }
      return ret;
    }
  }
  public enum LanguageTextFlow
  {
    Right, Left
  }
  public class Translator
  {
    public static LanguageTextFlow TextFlow { get; set; } = LanguageTextFlow.Right;
    public LanguageCode LanguageCode { get; set; } = LanguageCode.en;
    private BoJankEnumDataTable<LanguageCode, Phrase, String> _translateDataTable = null;
    public Translator()
    {
      //No, we don't adhere to any CSV standard.
      try
      {
        FileLoc f = new FileLoc("phrases.csv", EmbeddedFolder.Root);
        var csvData = new BoJankEnumCSV<LanguageCode, Phrase, String>(f, (x) => { return x; });

        if (Gu.EngineConfig.Debug_PrintTranslationTable)
        {
          Gu.Log.Debug(csvData.DebugPrintTable().ToString());
        }

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
      if (p == Phrase.None)
      {
        return "";
      }
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
        dat = $"{dat}";
      }

      return dat;
    }
    private String GetDefaultUntranslated(Phrase p)
    {
      //Since it wasn't found, add not found asterisks.
      return $"*{p.ToString()}*";
    }
    private string? GetInlineTrans(LanguageCode c, Phrase p)
    {
      InlineTranslation att = p.GetAttribute<InlineTranslation>();
      if (att != null)
      {
        att.InlineTranslations.TryGetValue(c, out var s);
        return s;
      }
      return null;
    }

  }
}