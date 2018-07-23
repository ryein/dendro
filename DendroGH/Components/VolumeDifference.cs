using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace DendroGH {
    public class VolumeDifference : GH_Component {
        /// <summary>
        /// Initializes a new instance of the VolumeDifference class.
        /// </summary>
        public VolumeDifference () : base ("Volume Difference", "vDiff",
            "Perform a diference operation on a set of volumes",
            "Dendro", "Intersect") { }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams (GH_Component.GH_InputParamManager pManager) {
            pManager.AddGenericParameter ("Volume A", "A", "First Volume", GH_ParamAccess.item);
            pManager.AddGenericParameter ("Volumes B", "B", "Second Volume set", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams (GH_Component.GH_OutputParamManager pManager) {
            pManager.AddGenericParameter ("Result", "R", "Difference result", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance (IGH_DataAccess DA) {
            DendroVolume vBase = new DendroVolume ();
            List<DendroVolume> vSubtract = new List<DendroVolume> ();

            if (!DA.GetData (0, ref vBase)) return;
            if (!DA.GetDataList (1, vSubtract)) return;

            DendroVolume csg = vBase.BooleanDifference (vSubtract);

            if (!csg.IsValid) {
                AddRuntimeMessage (GH_RuntimeMessageLevel.Error, "CSG failed. Make sure all supplied volumes are valid");
                return;
            }

            DA.SetData (0, new VolumeGOO (csg));
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon {
            get {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return DendroGH.Properties.Resources.ico_bool_sub;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid {
            get { return new Guid ("538a2666-a714-4f34-a0a8-76097a8f0c11"); }
        }
    }
}