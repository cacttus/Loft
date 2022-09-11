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
    //https://saimana.com/list-of-country-locale-code/
    NA
    ,@sq_AL //  Albanian (Albania)
    ,@sq// Albanian	
    ,@ar_DZ // Arabic (Algeria)
    ,@ar_BH // Arabic (Bahrain)
    ,@ar_EG // Arabic (Egypt)
    ,@ar_IQ // Arabic (Iraq)
    ,@ar_JO // Arabic (Jordan)
    ,@ar_KW // Arabic (Kuwait)
    ,@ar_LB // Arabic (Lebanon)
    ,@ar_LY // Arabic (Libya)
    ,@ar_MA // Arabic (Morocco)
    ,@ar_OM // Arabic (Oman)
    ,@ar_QA // Arabic (Qatar)
    ,@ar_SA // Arabic (Saudi Arabia)
    ,@ar_SD // Arabic (Sudan)
    ,@ar_SY // Arabic (Syria)
    ,@ar_TN // Arabic (Tunisia)
    ,@ar_AE // Arabic (United Arab Emirates)
    ,@ar_YE // Arabic (Yemen)
    ,@ar // Arabic
    ,@be_BY // Belarusian (Belarus)
    ,@be // Belarusian
    ,@bn_IN // Bengali (India)
    ,@bn_BD // Bengali (Bangladesh)
    ,@bn // Bengali
    ,@bg_BG // Bulgarian (Bulgaria)
    ,@bg // Bulgarian
    ,@ca_ES // Catalan (Spain)
    ,@ca // Catalan
    ,@zh_CN // Chinese (China)
    ,@zh_HK // Chinese (Hong Kong)
    ,@zh_SG // Chinese (Singapore)
    ,@zh_TW // Chinese (Taiwan)
    ,@zh // Chinese
    ,@hr_HR // Croatian (Croatia)
    ,@hr // Croatian
    ,@cs_CZ // Czech (Czech Republic)
    ,@cs // Czech
    ,@da_DK // Danish (Denmark)
    ,@da // Danish
    ,@nl_BE // Dutch (Belgium)
    ,@nl_NL // Dutch (Netherlands)
    ,@nl // Dutch
    ,@en_AU // English (Australia)
    ,@en_CA // English (Canada)
    ,@en_IN // English (India)
    ,@en_IE // English (Ireland)
    ,@en_MT // English (Malta)
    ,@en_NZ // English (New Zealand)
    ,@en_PH // English (Philippines)
    ,@en_SG // English (Singapore)
    ,@en_ZA // English (South Africa)
    ,@en_GB // English (United Kingdom)
    ,@en_US // English (United States)
    ,@en // English
    ,@et_EE // Estonian (Estonia)
    ,@et // Estonian
    ,@fi_FI // Finnish (Finland)
    ,@fi // Finnish
    ,@fr_BE // French (Belgium)
    ,@fr_CA // French (Canada)
    ,@fr_FR // French (France)
    ,@fr_LU // French (Luxembourg)
    ,@fr_CH // French (Switzerland)
    ,@fr // French
    ,@de_AT // German (Austria)
    ,@de_DE // German (Germany)
    ,@de_LU // German (Luxembourg)
    ,@de_CH // German (Switzerland)
    ,@de // German
    ,@el_CY // Greek (Cyprus)
    ,@el_GR // Greek (Greece)
    ,@el // Greek
    ,@iw_IL // Hebrew (Israel)
    ,@iw // Hebrew
    ,@hi_IN // Hindi (India)
    ,@hu_HU // Hungarian (Hungary)
    ,@hu // Hungarian
    ,@is_IS // Icelandic (Iceland)
    ,@is // Icelandic
    ,@in_ID // Indonesian (Indonesia)
    ,@in // Indonesian
    ,@ga_IE // Irish (Ireland)
    ,@ga // Irish
    ,@it_IT // Italian (Italy)
    ,@it_CH // Italian (Switzerland)
    ,@it // Italian
    ,@ja_JP // Japanese (Japan)
    ,@ja_JP_JP // Japanese (Japan,JP)	
    ,@ja // Japanese
    ,@ko_KR // Korean (South Korea)
    ,@ko // Korean
    ,@lv_LV // Latvian (Latvia)
    ,@lv // Latvian
    ,@lt_LT // Lithuanian (Lithuania)
    ,@lt // Lithuanian
    ,@mk_MK // Macedonian (Macedonia)
    ,@mk // Macedonian
    ,@ms_MY // Malay (Malaysia)
    ,@ms // Malay
    ,@mt_MT // Maltese (Malta)
    ,@mt // Maltese
    ,@no_NO // Norwegian (Norway)
    ,@no_NO_NY // Norwegian (Norway,Nynorsk)	
    ,@no // Norwegian
    ,@pl_PL // Polish (Poland)
    ,@pl // Polish
    ,@pt_BR // Portuguese (Brazil)
    ,@pt_PT // Portuguese (Portugal)
    ,@pt // Portuguese
    ,@ro_RO // Romanian (Romania)
    ,@ro // Romanian
    ,@ru_RU // Russian (Russia)
    ,@ru // Russian
    ,@sr_BA // Serbian (Bosnia and Herzegovina)
    ,@sr_ME // Serbian (Montenegro)
    ,@sr_CS // Serbian (Serbia and Montenegro)
    ,@sr_RS // Serbian (Serbia)
    ,@sr // Serbian
    ,@sk_SK // Slovak (Slovakia)
    ,@sk // Slovak
    ,@sl_SI // Slovenian (Slovenia)
    ,@sl // Slovenian
    ,@es_AR // Spanish (Argentina)
    ,@es_BO // Spanish (Bolivia)
    ,@es_CL // Spanish (Chile)
    ,@es_CO // Spanish (Colombia)
    ,@es_CR // Spanish (Costa Rica)
    ,@es_DO // Spanish (Dominican Republic)
    ,@es_EC // Spanish (Ecuador)
    ,@es_SV // Spanish (El Salvador)
    ,@es_GT // Spanish (Guatemala)
    ,@es_HN // Spanish (Honduras)
    ,@es_MX // Spanish (Mexico)
    ,@es_NI // Spanish (Nicaragua)
    ,@es_PA // Spanish (Panama)
    ,@es_PY // Spanish (Paraguay)
    ,@es_PE // Spanish (Peru)
    ,@es_PR // Spanish (Puerto Rico)
    ,@es_ES // Spanish (Spain)
    ,@es_US // Spanish (United States)
    ,@es_UY // Spanish (Uruguay)
    ,@es_VE // Spanish (Venezuela)
    ,@es // Spanish
    ,@sv_SE // Swedish (Sweden)
    ,@sv // Swedish
    ,@th_TH // Thai (Thailand)
    ,@th_TH_TH // Thai (Thailand,TH)	
    ,@th // Thai
    ,@tr_TR // Turkish (Turkey)
    ,@tr // Turkish
    ,@uk_UA // Ukrainian (Ukraine)
    ,@uk // Ukrainian
    ,@vi_VN // Vietnamese (Vietnam)
    ,@vi // Vietnamese
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