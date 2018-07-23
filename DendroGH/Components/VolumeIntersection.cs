using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace DendroGH {
    public class VolumeIntersection : GH_Component {
        /// <summary>
        /// Initializes a new instance of the VolumeIntersection class.
        /// </summary>
        public VolumeIntersection () : base ("Volume Intersection", "vInt",
            "Perform a intersection operation on a set of volumes",
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
            pManager.AddGenericParameter ("Result", "R", "Intersection result", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance (IGH_DataAccess DA) {
            DendroVolume vBase = new DendroVolume ();
            List<DendroVolume> vIntersect = new List<DendroVolume> ();

            if (!DA.GetData (0, ref vBase)) return;
            if (!DA.GetDataList (1, vIntersect)) return;

            DendroVolume csg = vBase.BooleanIntersection (vIntersect);

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
                return DendroGH.Properties.Resources.ico_bool_int;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid {
            get { return new Guid ("52335925-f117-419c-a3f4-7e6da9bf9366"); }
        }
    }
}