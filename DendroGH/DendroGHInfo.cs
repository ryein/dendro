using System;
using System.Drawing;
using System.Reflection;
using Grasshopper.Kernel;

[assembly: Grasshopper.Kernel.GH_Loading(GH_LoadingDemand.ForceDirect)]

namespace DendroGH
{
    public class DendroGHInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "DendroGH";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return null;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("e355f38c-064c-4317-a17c-499c5a2fcded");
            }
        }
        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "ecr labs";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "dev@ecrlabs.com";
            }
        }
        public override string Version
        {
            get
            {
                // keep the version of the plugin in sync with AssemblyVersion
                return AssemblyVersion;
            }
        }
        public override string AssemblyVersion
        {
            get
            {
                // make use of the AssemblyVersion (defined in AssemblyInfo.cs)
                var assembly = Assembly.GetExecutingAssembly();
                var assemblyName = new AssemblyName(assembly.FullName);
                return assemblyName.Version.ToString();
            }
        }
    }
}
