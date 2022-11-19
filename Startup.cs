namespace ReportBuilder.Web.Models
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            StaticConfig = configuration;
        }

        public IConfiguration Configuration { get; }
        public static IConfiguration StaticConfig { get;  set; }  //<--- Add this line manually
    }
}
