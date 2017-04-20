using System;
using System.Text.RegularExpressions;
using Exortech.NetReflector;
using ThoughtWorks.CruiseControl.Remote;
using System.Globalization;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Reflection;

namespace ThoughtWorks.CruiseControl.Core.Label
{
    /// <summary>
    /// <para>
    /// Allows CCNet create custom code-generated labels
    /// </para>
    /// <para>
    /// You can do this by specifying your own configuration of the default labeller in your project.
    /// </para>
    /// </summary>
    /// <title>Custom Labeller</title>
    /// <version>1.0</version>
    /// <example>
    /// <code>
    /// &lt;labeller type="customlabeller"&gt;
    /// &lt;cscode&gt;1&lt;/cscode&gt;
    /// &lt;/labeller&gt;
    /// </code>
    /// </example>
    [ReflectorType("customlabeller")]
    public class CustomLabeller
        : LabellerBase
    {
        /// <summary>
        /// Generates the specified integration result.	
        /// </summary>
        /// <param name="integrationResult">The integration result.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public override string Generate(IIntegrationResult integrationResult)
        {
            MethodInfo method = this.CreateFunction(this.CsCode);
            string ret = (string)method.Invoke(null, new object[1] { integrationResult });
            return ret;
        }

        [ReflectorProperty("cscode", Required = true)]
        public string CsCode { get; set; }

        private string CSCodeWrapper
        {
            get
            {
                return @"

using System;
using ThoughtWorks.CruiseControl.Core;
using ThoughtWorks.CruiseControl.Remote;
            
namespace CustomLabelerGeneratorUserFunctions
{                
    public class CustomLabelerGenerator
    {                
        public static string Generate(ThoughtWorks.CruiseControl.Core.IIntegrationResult integrationResult)
        {
            string ret = ""0.0.0.0"";
            <customCodeForReplace>
            return ret;
        }
    }
}
";
            }
        }

        public MethodInfo CreateFunction(string function)
        {
            string finalCode = CSCodeWrapper.Replace("<customCodeForReplace>", function);

            CSharpCodeProvider provider = new CSharpCodeProvider();
            var parameters = new CompilerParameters();
            parameters.ReferencedAssemblies.Add(Assembly.GetExecutingAssembly().Location);
            parameters.ReferencedAssemblies.Add(typeof(ThoughtWorks.CruiseControl.Remote.IntegrationStatus).Assembly.Location);
            parameters.GenerateInMemory = true;
            parameters.GenerateExecutable = false;
            CompilerResults results = provider.CompileAssemblyFromSource(parameters, finalCode);

            Type binaryFunction = results.CompiledAssembly.GetType("CustomLabelerGeneratorUserFunctions.CustomLabelerGenerator");
            return binaryFunction.GetMethod("Generate");
        }
    }
}
