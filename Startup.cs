namespace ReportBuilder.Web.Models
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            StaticConfig = configuration; //<--- Add this line manually
        }

        public IConfiguration Configuration { get; }
        public static IConfiguration StaticConfig { get;  set; }  //<--- Add this line manually
    }
}
