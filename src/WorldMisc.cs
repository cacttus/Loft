namespace PirateCraft{
  public class DayNightCycle
  {
    //                  + Noon
    // 
    // Dusk +                        + Dawn    < sun direction
    //
    //                  + Midngiht

    private double DayLengthSeconds = 60;//60;// 60.0f * 5.0f;
    private double NightLengthSeconds = 60;//60;//60.0f * 5.0f;

    public double StarOrCloud_Blend { get; private set; } = 0; //1 = day,  -1 = night

    public double DayTime_Seconds { get; private set; } = 0;// Time in seconds
    public double DayLength_Seconds { get { return DayLengthSeconds + NightLengthSeconds; } }
    public dvec3 MoonDir { get; private set; } = new dvec3(0, 0, 1);//Direction of moon towards earth or inverse of sun
    public dvec3 ActiveLightDir { get; private set; } = new dvec3(0, 0, 1);//Depending on whether it is night, or not this is the direction of sun / moon towards earth
    public const float SkyRadius = 400.0f;

    public float DayQuad0 { get; private set; } = 0;
    public float DayQuad1 { get; private set; } = 1;

    public dvec3 SkyColor;
    public dvec3 LightColor;
    //https://yorktown.cbe.wwu.edu/sandvig/shared/NetColors.aspx
    private dvec3 Sky_NoonColor = new dvec3(vec4.FromHex("#FAFAD200").xyz);//Goldenrod
    private dvec3 Sky_DuskColor = new dvec3(vec4.FromHex("#FF450000").xyz);//Gold FFD70000
    private dvec3 Sky_DawnColor = new dvec3(vec4.FromHex("#FF450000").xyz);//Gold
    private dvec3 Sky_MidnightColor = new dvec3(vec4.FromHex("#16163000").xyz);//Midnightblue

    private dvec3 Light_NoonColor = new dvec3(vec4.FromHex("#FAFAD200").xyz);//light goldenrod
    private dvec3 Light_MidnightColor = new dvec3(vec4.FromHex("#E6E6FA00").xyz); //a very faint lavender blue
    private dvec3 Light_DuskColor { get { return (Light_NoonColor + Light_MidnightColor) * 0.5; } }//Gold FFD70000
    private dvec3 Light_DawnColor { get { return Light_DuskColor; } }//orangered

    public bool IsDay
    {
      get
      {
        return DayTime_Seconds < DayLengthSeconds;
      }
    }

    public void Update(double dt)
    {
      DayTime_Seconds = (DayTime_Seconds + dt) % DayLength_Seconds;

      double time01 = DayTime_Seconds / DayLength_Seconds;
      MoonDir = new dvec3(Math.Cos(time01 * MathUtils.M_2PI), Math.Sin(time01 * MathUtils.M_2PI), 0);

      ActiveLightDir = IsDay ? (MoonDir) : MoonDir * -1.0f;

      double d2 = DayLengthSeconds * 0.5;
      double n2 = NightLengthSeconds * 0.5;

      double a = 0;
      double b = d2;
      double c = d2 + d2;
      double d = d2 + d2 + n2;
      double e = d2 + d2 + n2 + n2;

      dvec3 c_sky_a = dvec3.Zero, c_sky_b = dvec3.Zero;
      dvec3 c_light_a = dvec3.Zero, c_light_b = dvec3.Zero;
      double t = 0;

      double dawndusk_duration_power = 4;

      if (DayTime_Seconds >= a && DayTime_Seconds < b)
      {
        c_sky_a = Sky_DawnColor;
        c_sky_b = Sky_NoonColor;
        c_light_a = Light_DawnColor;
        c_light_b = Light_NoonColor;
        t = (DayTime_Seconds - a) / (b - a);
        t = 1 - (Math.Pow(1 - t, dawndusk_duration_power));
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
        t = (Math.Pow(t, dawndusk_duration_power));
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
        t = 1 - (Math.Pow(1 - t, dawndusk_duration_power));
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
        t = (Math.Pow(t, dawndusk_duration_power));
        DayQuad0 = 1;
        DayQuad1 = 0;
      }

      SkyColor = dvec3.CosineInterpolate(c_sky_a, c_sky_b, t);
      LightColor = dvec3.CosineInterpolate(c_light_a, c_light_a, t);

      //0 star 1 cloud
      double daynight_tex_blendspd = 4;
      double dot = MoonDir.dot(new dvec3(0, 1, 0));
      dot = Math.Pow(Math.Abs(dot), daynight_tex_blendspd) * Math.Sign(dot);
      if (dot < 0)
      {
        StarOrCloud_Blend = MathUtils.Ease(0, -1, dot) * 0.5 + 0.5;
      }
      else
      {
        StarOrCloud_Blend = MathUtils.Ease(0, 1, dot) * 0.5 + 0.5;
      }
    }
  }

}