using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace DendroGH {
    public class VolumeToMesh : GH_Component {
        /// <summary>
        /// Initializes a new instance of the VolumeToMesh class.
        /// </summary>
        public VolumeToMesh () : base ("Volume to Mesh", "mVolume",
            "Create a mesh that approximates volume geometry",
            "Dendro", "Convert") { }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams (GH_Component.GH_InputParamManager pManager) {
            pManager.AddGenericParameter ("Volume", "V", "Volume geometry", GH_ParamAccess.item);
            pManager.AddGenericParameter ("Volume Settings", "S", "Global Volume Settings to be used", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams (GH_Component.GH_OutputParamManager pManager) {
            pManager.AddMeshParameter ("Mesh", "M", "Mesh conversion of volume", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance (IGH_DataAccess DA) {
            DendroVolume mVolume = new DendroVolume ();
            DendroSettings vSettings = new DendroSettings ();

            if (!DA.GetData (0, ref mVolume)) return;
            if (!DA.GetData (1, ref vSettings)) return;

            if (!mVolume.IsValid) {
                AddRuntimeMessage (GH_RuntimeMessageLevel.Error, "Volume is not valid");
                return;
            }

            mVolume.UpdateDisplay (vSettings);

            DA.SetData (0, mVolume.Display);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon {
            get {
                return DendroGH.Properties.Resources.ico_vox_mesh;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid {
            get { return new Guid ("858d89ff-5854-4e5f-aeb2-0e43e580835e"); }
        }
    }
}