namespace Loft{
  public class DayNightCycle
  {
    //                  + Noon
    // 
    // Dusk +                        + Dawn    < sun direction
    //
    //                  + Midngiht

    private float DayLengthSeconds = 60;//60;// 60.0f * 5.0f;
    private float NightLengthSeconds = 60;//60;//60.0f * 5.0f;
    public float StarOrCloud_Blend { get; private set; } = 0; //1 = day,  -1 = night
    public float DayTime_Seconds { get; private set; } = 0;// Time in seconds
    public float DayLength_Seconds { get { return DayLengthSeconds + NightLengthSeconds; } }
    public vec3 MoonDir { get; private set; } = new vec3(0, 0, 1);//Direction of moon towards earth or inverse of sun
    public vec3 ActiveLightDir { get; private set; } = new vec3(0, 0, 1);//Depending on whether it is night, or not this is the direction of sun / moon towards earth
    public const float SkyRadius = 400.0f;

    public float DayQuad0 { get; private set; } = 0;
    public float DayQuad1 { get; private set; } = 1;

    public vec3 SkyColor;
    public vec3 LightColor;
    //https://yorktown.cbe.wwu.edu/sandvig/shared/NetColors.aspx
    private vec3 Sky_NoonColor = OffColor.Goldenrod.xyz;// new vec3(vec4.FromHex("#FAFAD200").xyz);//Goldenrod
    private vec3 Sky_DuskColor = OffColor.Gold.rgb;// new vec3(vec4.FromHex("#FF450000").xyz);//Gold FFD70000
    private vec3 Sky_DawnColor = OffColor.Gold.rgb;// new vec3(vec4.FromHex("#FF450000").xyz);//Gold
    private vec3 Sky_MidnightColor = OffColor.MidnightBlue.rgb;// new vec3(vec4.FromHex("#16163000").xyz);//Midnightblue

    private vec3 Light_NoonColor = OffColor.LightGoldenrodYellow.rgb;//light goldenrod
    private vec3 Light_MidnightColor =  vec4.FromHex("#E6E6FA00").rgb; //a very faint lavender blue
    private vec3 Light_DuskColor { get { return (Light_NoonColor + Light_MidnightColor) * 0.5f; } }//Gold FFD70000
    private vec3 Light_DawnColor { get { return Light_DuskColor; } }//orangered

    public bool IsDay
    {
      get
      {
        return DayTime_Seconds < DayLengthSeconds;
      }
    }

    public void Update(double dtt)
    {
      float dt = (float)dtt;
      DayTime_Seconds = (DayTime_Seconds + dt) % DayLength_Seconds;

      float time01 = DayTime_Seconds / DayLength_Seconds;
      MoonDir = new vec3((float)Math.Cos(time01 * MathUtils.M_2PI), (float)Math.Sin(time01 * MathUtils.M_2PI), 0);

      ActiveLightDir = IsDay ? (MoonDir) : MoonDir * -1.0f;

      float d2 = DayLengthSeconds * 0.5f;
      float n2 = NightLengthSeconds * 0.5f;
      float a = 0;
      float b = d2;
      float c = d2 + d2;
      float d = d2 + d2 + n2;
      float e = d2 + d2 + n2 + n2;

      vec3 c_sky_a = vec3.Zero, c_sky_b = vec3.Zero;
      vec3 c_light_a = vec3.Zero, c_light_b = vec3.Zero;
      float t = 0;

      float dawndusk_duration_power = 4;

      if (DayTime_Seconds >= a && DayTime_Seconds < b)
      {
        c_sky_a = Sky_DawnColor;
        c_sky_b = Sky_NoonColor;
        c_light_a = Light_DawnColor;
        c_light_b = Light_NoonColor;
        t = (DayTime_Seconds - a) / (b - a);
        t = 1 - (float)(Math.Pow(1 - t, dawndusk_duration_power));
        DayQuad0 = 0;
        DayQuad1 = 1;
      }
      else if (DayTime_Seconds >= b && DayTime_Seconds < c)
      {
        c_sky_a = Sky_NoonColor;
        c_sky_b = Sky_DuskColor;
        c_light_a = Light_NoonColor;
        c_light_b = Light_DuskColor;
        t = (DayTime_Seconds - b) / (c - b);
        t = (float)(Math.Pow(t, dawndusk_duration_power));
        DayQuad0 = 1;
        DayQuad1 = 0;
      }
      else if (DayTime_Seconds >= c && DayTime_Seconds < d)
      {
        c_sky_a = Sky_DuskColor;
        c_sky_b = Sky_MidnightColor;
        c_light_a = Light_DuskColor;
        c_light_b = Light_MidnightColor;
        t = (DayTime_Seconds - c) / (d - c);
        t = 1 - (float)(Math.Pow(1 - t, dawndusk_duration_power));
        DayQuad0 = 0;
        DayQuad1 = 1;
      }
      else if (DayTime_Seconds >= d && DayTime_Seconds < e)
      {
        c_sky_a = Sky_MidnightColor;
        c_sky_b = Sky_DawnColor;
        c_light_a = Light_MidnightColor;
        c_light_b = Light_DawnColor;
        t = (DayTime_Seconds - d) / (e - d);
        t = (float)(Math.Pow(t, dawndusk_duration_power));
        DayQuad0 = 1;
        DayQuad1 = 0;
      }

      SkyColor = vec3.CosineInterpolate(c_sky_a, c_sky_b, t);
      LightColor = vec3.CosineInterpolate(c_light_a, c_light_a, t);

      //0 star 1 cloud
      float daynight_tex_blendspd = 4;
      float dot = MoonDir.dot(new vec3(0, 1, 0));
      dot = (float)Math.Pow(Math.Abs(dot), daynight_tex_blendspd) *(float) Math.Sign(dot);
      if (dot < 0)
      {
        StarOrCloud_Blend = (float)MathUtils.Ease(0, -1, dot) * 0.5f + 0.5f;
      }
      else
      {
        StarOrCloud_Blend = (float)MathUtils.Ease(0, 1, dot) * 0.5f + 0.5f;
      }
    }
  }

}