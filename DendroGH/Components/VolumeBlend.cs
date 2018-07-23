using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace DendroGH {
    public class VolumeBlend : GH_Component {
        /// <summary>
        /// Initializes a new instance of the VolumeBlend class.
        /// </summary>
        public VolumeBlend() : base ("Volume Blend", "vBlend",
            "Blend between two volumes",
            "Dendro", "Filters") { }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams (GH_Component.GH_InputParamManager pManager) {
            pManager.AddGenericParameter ("Volume A", "A", "First Volume", GH_ParamAccess.item);
            pManager.AddGenericParameter ("Volume B", "B", "Second Volume", GH_ParamAccess.item);
            pManager.AddNumberParameter("Parameter", "t", "Parameter of blend domain to evaluate (normalized 0-1)", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("End time", "E", "End time of evaluations", GH_ParamAccess.item, 10);
            pManager.AddGenericParameter ("Mask", "M", "(Optional) Mask for filter operations", GH_ParamAccess.item);
            pManager[4].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams (GH_Component.GH_OutputParamManager pManager) {
            pManager.AddGenericParameter("Result", "R", "Morph result", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance (IGH_DataAccess DA) {
            DendroVolume vBegin = new DendroVolume ();
            DendroVolume vEnd = new DendroVolume ();
            double vParam = 0.0;
            double vTime = 0.0;
            DendroMask vMask = new DendroMask ();

            if (!DA.GetData (0, ref vBegin)) return;
            if (!DA.GetData (1, ref vEnd)) return;
            if (!DA.GetData(2, ref vParam)) return;
            if (!DA.GetData(3, ref vTime)) return;
            DA.GetData (4, ref vMask);

            if (vMask == null) return;

            DendroVolume blend = new DendroVolume();

            if (vMask.IsValid)
            {
                blend = vBegin.Blend(vEnd, vParam, vTime, vMask);
            }
            else
            {
                blend = vBegin.Blend(vEnd, vParam, vTime);
            }

            if (!blend.IsValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Blend failed. Make sure all supplied volumes are valid");
                return;
            }

            DA.SetData(0, new VolumeGOO(blend));
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon {
            get {
                return DendroGH.Properties.Resources.ico_morph;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid {
            get { return new Guid ("001791c1-b549-4282-b800-0de064a76d61"); }
        }
    }
}