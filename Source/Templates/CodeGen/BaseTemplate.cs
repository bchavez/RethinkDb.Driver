using System.Text;
using RazorGenerator.Templating;
using Templates.CodeGen.Util;

namespace Templates.CodeGen
{
    public class BaseTemplate : RazorTemplateBase
    {
        public string Class(string str)
        {
            return str.ClassName();
        }

        public string Method(string str)
        {
            return str.MethodName();
        }

        public string Property(string str)
        {
            return str.PropertyName();
        }

        public string ArgumentVariable(string str)
        {
            return str.ArgumentName();
        }

        public string ArgumentType(string str)
        {
            return str.ArgumentTypeName();
        }

        public RazorTemplateBase ChildTemplate { get; set; }

        public override string RenderSection(string name)
        {
            //render child first
            if( this.ChildTemplate != null && this.ChildTemplate.sections.ContainsKey(name) )
            {
                //check child if they have a section.

                //render it. because we need it
                var content = this.content;
                this.ChildTemplate.Clear();
                this.ChildTemplate.sections[name]();
                var sectionContent = this.ChildTemplate.genEnv.ToString();
                this.content = content;
                return sectionContent;
            }
            else
            {
                return base.RenderSection(name);
            }
        }

        public virtual RazorTemplateBase UseParentLayout(RazorTemplateBase child)
        {
            return null;
        }
    }
}