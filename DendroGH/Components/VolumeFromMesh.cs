using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace DendroGH {
    public class VolumeFromMesh : GH_Component {
        /// <summary>
        /// Initializes a new instance of the VolumeFromMesh class.
        /// </summary>
        public VolumeFromMesh () : base ("Mesh to Volume", "vMesh",
            "Create a volume that approximates mesh geometry",
            "Dendro", "Convert") { }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams (GH_Component.GH_InputParamManager pManager) {
            pManager.AddMeshParameter ("Mesh", "M", "Base mesh", GH_ParamAccess.item);
            pManager.AddGenericParameter ("Settings", "S", "Settings for converting different geometry types to and from volumes", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams (GH_Component.GH_OutputParamManager pManager) {
            pManager.AddGenericParameter ("Volume", "V", "Volume geometry", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance (IGH_DataAccess DA) {
            Mesh vMesh = new Mesh ();
            DendroSettings vSettings = new DendroSettings ();

            if (!DA.GetData (0, ref vMesh)) return;
            if (!DA.GetData (1, ref vSettings)) return;

            DendroVolume volume = new DendroVolume (vMesh, vSettings);

            if (!volume.IsValid) {
                AddRuntimeMessage (GH_RuntimeMessageLevel.Error, "Conversion failed. Make sure you supplied a valid mesh and correct settings");
                return;
            }

            DA.SetData(0, new VolumeGOO(volume));
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon {
            get {
                return DendroGH.Properties.Resources.ico_mesh_vox;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid {
            get { return new Guid ("d6403738-1061-47a6-b143-1f9cd72d8188"); }
        }
    }
}