using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace System.Web.WebPages
{
    public class HelperResult
    {
        private readonly Action<TextWriter> writer;

        public HelperResult(Action<TextWriter> writer)
        {
            this.writer = writer;
        }

        public override string ToString()
        {
            writer(null);
            return string.Empty;
        }
    }

}

namespace RazorGenerator.Templating
{
    public class RazorTemplateBase
    {
        public RazorTemplateBase Layout { get; set; }
        protected string content;
        internal StringBuilder genEnv = new StringBuilder();

        public virtual void Execute()
        {
        }

        public void WriteLiteralTo(TextWriter writer, string textToAppend)
        {
            if( string.IsNullOrEmpty(textToAppend) )
            {
                return;
            }
            genEnv.Append(textToAppend);
        }

        public void WriteLiteral(string textToAppend)
        {
            if( string.IsNullOrEmpty(textToAppend) )
            {
                return;
            }
            genEnv.Append(textToAppend); ;
        }

        public void Write(object value)
        {
            if( ( value == null ) )
            {
                return;
            }

            WriteLiteral(Convert.ToString(value, CultureInfo.InvariantCulture));
        }

        public string RenderBody()
        {
            return content;
        }

        public string TransformText()
        {
            Execute();
            if( Layout != null )
            {
                Layout.content = genEnv.ToString();
                return Layout.TransformText();
            }
            else
            {
                return genEnv.ToString();
            }
        }

        public void Clear()
        {
            genEnv.Clear();

            if( Layout != null )
            {
                Layout.content = "";
            }
        }

        public void WriteTo(TextWriter writer, object value)
        {
            genEnv.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
            //writer.Write();
        }

        internal Dictionary<string, Action> sections = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase);

        public virtual void DefineSection(string name, Action content)
        {
            if( this.sections.ContainsKey(name) )
            {
                //define it oncetarget[name] = content;
                return;
            }
            this.sections.Add(name, content);
        }

        public virtual string RenderSection(string name)
        {
            if( this.sections.ContainsKey(name) )
            {
                this.sections[name]();
            }
            return string.Empty;
        }
    }
}