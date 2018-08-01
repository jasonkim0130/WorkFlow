using System.Web;
using System.Web.Optimization;

namespace WorkFlow
{
    public class BundleConfig
    {
        // For more information on bundling, visit https://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js",
                        "~/Scripts/jquery-ui.drag.date.auto.min.js",
                        "~/Scripts/upload/jquery.ui.widget.js",
                        "~/Scripts/upload/jquery.iframe-transport.js",
                        "~/Scripts/upload/jquery.fileupload.js",
                        "~/Scripts/upload/jquery.fileupload-process.js",
                        "~/Scripts/jquery-ui-timepicker-addon.js",
                        "~/Scripts/cainiaoprint-{version}.js",
                        "~/Scripts/jquery.jqprint-{version}.js",
                        "~/Scripts/moment.js",
                        "~/Scripts/daterangepicker.js",
                        "~/Scripts/site-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/mobile-jquery").Include(
                      "~/Scripts/jquery-{version}.js",
                      "~/Scripts/mobile-site-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.unobtrusive-ajax*",
                        "~/Scripts/jquery.validate*"));

            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                      "~/Scripts/bootstrap.js",
                      "~/Scripts/respond.js",
                      "~/Scripts/bootstrap-select.js"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap-select").Include(
                      "~/Scripts/bootstrap-select.js"));

            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/bootstrap.css",
                      "~/Content/jquery-ui-drag-auto-date.css",
                      "~/Content/jquery-ui-timepicker-addon.css",
                      "~/Content/site-{version}.css",
                      "~/Content/bootstrap-select.css"));

            bundles.Add(new StyleBundle("~/Content/mobile-css").Include(
                "~/Content/bootstrap.css",
                "~/Content/Mobile-site-{version}.css"));

            bundles.Add(new StyleBundle("~/Content/ec/refund-css").Include(
                    "~/Areas/ECOrderReturn/Content/css/index.css"
                ));
            //           "~/Scripts/upload/jquery.fileupload.js"));
#if DEBUG
            BundleTable.EnableOptimizations = false;
#else
             BundleTable.EnableOptimizations = true;
#endif
        }
    }
}
