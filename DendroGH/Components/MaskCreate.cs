using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace DendroGH {
    public class MaskCreate : GH_Component {
        /// <summary>
        /// Initializes a new instance of the MaskFromVolume class.
        /// </summary>
        public MaskCreate () : base ("Create Mask", "Mask",
            "Create a mask from a volume to be used in volume filter operations",
            "Dendro", "Filters") { }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams (GH_Component.GH_InputParamManager pManager) {
            pManager.AddGenericParameter ("Volume", "V", "Volume geometry", GH_ParamAccess.item);
            pManager.AddNumberParameter ("Min Value", "A", "Minimum value of the mask to be used for the derivation of a smooth alpha value", GH_ParamAccess.item, 0.0);
            pManager.AddNumberParameter ("Max Value", "B", "Maximum value of the mask to be used for the derivation of a smooth alpha value", GH_ParamAccess.item, 1.0);
            pManager.AddBooleanParameter ("Mask Invert", "I", "Invert the mask values", GH_ParamAccess.item, true);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams (GH_Component.GH_OutputParamManager pManager) {
            pManager.AddGenericParameter ("Mask", "M", "Mask for volume filter operations", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance (IGH_DataAccess DA) {
            DendroVolume volume = new DendroVolume ();
            double min = 0.0;
            double max = 1.0;
            bool invert = false;

            if (!DA.GetData (0, ref volume)) return;
            if (!DA.GetData (1, ref min)) return;
            if (!DA.GetData (2, ref max)) return;
            if (!DA.GetData (3, ref invert)) return;

            if(max < min)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Max value must be larger than min value");
                return;
            }

            DendroMask mask = new DendroMask (volume);
            mask.Invert = invert;
            mask.Min = min;
            mask.Max = max;

            if (!mask.IsValid) {
                AddRuntimeMessage (GH_RuntimeMessageLevel.Error, "Mask creation failed. Is your volume volid?");
                return;
            }

            DA.SetData (0, new MaskGOO (mask));

        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon {
            get {
                return DendroGH.Properties.Resources.ico_mask;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid {
            get { return new Guid ("97e5d356-a481-4a1d-a11f-fb78bdcbb86f"); }
        }
    }
}