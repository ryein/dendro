using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace DendroGH {
    public class VolumeOffset : GH_Component {
        /// <summary>
        /// Initializes a new instance of the DendroOffset class.
        /// </summary>
        public VolumeOffset () : base ("Offset Volume", "vOffset",
            "Offset a volume by a fixed amount",
            "Dendro", "Filters") { }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams (GH_Component.GH_InputParamManager pManager) {
            pManager.AddGenericParameter ("Volume", "V", "Volume geometry", GH_ParamAccess.item);
            pManager.AddNumberParameter ("Distance", "D", "Offset distance", GH_ParamAccess.item);
            pManager.AddGenericParameter ("Mask", "M", "(Optional) Mask for filter operations", GH_ParamAccess.item);
            pManager[2].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams (GH_Component.GH_OutputParamManager pManager) {
            pManager.AddGenericParameter ("Volume", "V", "Offset volume", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance (IGH_DataAccess DA) {
            DendroVolume volume = new DendroVolume ();
            DendroMask vMask = new DendroMask ();
            double oAmount = 0.0;

            if (!DA.GetData (0, ref volume)) return;
            if (!DA.GetData (1, ref oAmount)) return;
            DA.GetData (2, ref vMask);

            if (vMask == null) return;

            DendroVolume offset = new DendroVolume();

            if (vMask.IsValid) {
                offset = volume.Offset (oAmount, vMask);
            }
            else {
                offset = volume.Offset (oAmount);
            }

            if (!offset.IsValid) {
                AddRuntimeMessage (GH_RuntimeMessageLevel.Error, "Offset failed. Make sure all supplied volumes are valid");
                return;
            }

            DA.SetData (0, new VolumeGOO (offset));
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon {
            get {
                return DendroGH.Properties.Resources.ico_offset;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid {
            get { return new Guid ("bff5520d-152a-4a1f-adda-2355f2478b9c"); }
        }
    }
}