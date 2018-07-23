using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace DendroGH {
    public class VolumeUnion : GH_Component {
        /// <summary>
        /// Initializes a new instance of the ovdbUnion class.
        /// </summary>
        public VolumeUnion () : base ("Volume Union", "vUnion",
            "Perform a union operation on a set of volumes",
            "Dendro", "Intersect") { }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams (GH_Component.GH_InputParamManager pManager) {
            pManager.AddGenericParameter ("Volumes", "V", "Volumes to union", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams (GH_Component.GH_OutputParamManager pManager) {
            pManager.AddGenericParameter ("Result", "R", "Union result", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance (IGH_DataAccess DA) {
            List<DendroVolume> vUnion = new List<DendroVolume> ();

            if (!DA.GetDataList (0, vUnion)) return;

            if (vUnion.Count < 1) {
                AddRuntimeMessage (GH_RuntimeMessageLevel.Error, "Need to supply a value");
                return;
            }

            DendroVolume csg = new DendroVolume();

            if (vUnion.Count == 1) {
                csg = new DendroVolume (vUnion[0]);
            }
            else {
                csg = vUnion[0].BooleanUnion (vUnion);
            }

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
                return DendroGH.Properties.Resources.ico_bool_union;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid {
            get { return new Guid ("dc9d71b6-f859-4b95-ad2a-608bee0f6b23"); }
        }
    }
}