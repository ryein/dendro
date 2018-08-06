using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace DendroGH {
    public class VolumeSmooth : GH_Component {
        /// <summary>
        /// Initializes a new instance of the ovdbSmooth class.
        /// </summary>
        public VolumeSmooth () : base ("Smooth Volume", "vSmooth",
            "Apply smoothing to volume",
            "Dendro", "Filters") { }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams (GH_Component.GH_InputParamManager pManager) {
            pManager.AddGenericParameter ("Volume", "V", "Volume geometry", GH_ParamAccess.item);
            pManager.AddIntegerParameter ("Width", "W", "(Optional) Width of smoothing. This value acts as a multiplier.", GH_ParamAccess.item, 1);
            pManager.AddIntegerParameter ("Type", "T", "0 - gaussian, 1 - laplacian, 2 - mean, 3 - median", GH_ParamAccess.item, 2);
            pManager.AddIntegerParameter ("Iterations", "I", "Number of smoothing iterations", GH_ParamAccess.item, 1);
            pManager.AddGenericParameter ("Mask", "M", "(Optional) Mask for filter operations", GH_ParamAccess.item);
            pManager[4].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams (GH_Component.GH_OutputParamManager pManager) {
            pManager.AddGenericParameter ("Volume", "V", "Smoothed volume", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance (IGH_DataAccess DA) {
            DendroVolume sVolume = new DendroVolume ();
            DendroMask vMask = new DendroMask ();
            int sType = 1;
            int sIterations = 1;
            int sWidth = 1;

            if (!DA.GetData (0, ref sVolume)) return;
            if (!DA.GetData (1, ref sWidth)) return;
            if (!DA.GetData (2, ref sType)) return;
            if (!DA.GetData (3, ref sIterations)) return;
            DA.GetData (4, ref vMask);

            if (vMask == null) return;

            DendroVolume smooth = new DendroVolume();

            if (vMask.IsValid)
            {
                smooth = sVolume.Smooth(sType, sIterations, vMask, sWidth);
            }
            else
            {
                smooth = sVolume.Smooth(sType, sIterations, sWidth);
            }

            if (!smooth.IsValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Smooth failed. Make sure all supplied volumes are valid");
                return;
            }

            DA.SetData (0, new VolumeGOO (smooth));

        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon {
            get {
                return DendroGH.Properties.Resources.ico_smooth;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid {
            get { return new Guid ("cc7da05b-aea7-47ce-b74d-6d84d25ebac3"); }
        }
    }
}