using System;
using System.Web;

namespace SenseNet.Chello
{
    public class DumperModule : IHttpModule
    {
        public void Init(HttpApplication context)
        {
            context.BeginRequest += OnBeginRequest;
            context.EndRequest += OnEndRequest;
        }

        public void Dispose()
        {
        }

        void OnBeginRequest(object sender, EventArgs e)
        {
            //log full request
            Dumper.DumpRequest(((HttpApplication)sender).Context.Request);

            //set filter stream to be able to log the response later
            var response = HttpContext.Current.Response;
            var filter = new OutputFilterStream(response.Filter);
            response.Filter = filter;
        }

        void OnEndRequest(object sender, EventArgs e)
        {
            var app = sender as HttpApplication;
            if (app == null)
                return;

            Dumper.DumpResponse(app.Response, app.Request);
        }

    }
}
