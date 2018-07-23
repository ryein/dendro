using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace DendroGH {
    public class SettingsParam : GH_Param<SettingsGOO> {
        /// <summary>
        /// Initializes a new instance of the SettingsParam class.
        /// </summary>
        public SettingsParam () : base (new GH_InstanceDescription ("Volume Settings", "vSettings", "Contains a collection of volume settings", "Dendro", "_Types")) { }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon {
            get {
                return DendroGH.Properties.Resources.ico_param_set;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid {
            get { return new Guid ("5fe922b3-f95c-4ea6-8f0a-e7bb961b4d90"); }
        }
    }
}