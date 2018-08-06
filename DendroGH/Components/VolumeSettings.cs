using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace DendroGH {
    public class VolumeSettings : GH_Component {
        /// <summary>
        /// Initializes a new instance of the VolumeSettings class.
        /// </summary>
        public VolumeSettings () : base ("Create Settings", "vSettings",
            "Settings for converting different geometry types to and from volumes",
            "Dendro", "Convert") { }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams (GH_Component.GH_InputParamManager pManager) {
            pManager.AddNumberParameter ("Voxel Size", "S", "Size of voxels in the output volume", GH_ParamAccess.item, 1);
            pManager.AddNumberParameter ("Bandwidth", "B", "Desired radius in voxel units around the surface", GH_ParamAccess.item, 1);
            pManager.AddNumberParameter ("Isovalue", "I", "Crossing point of the volume that is considered the surface", GH_ParamAccess.item, 0.0);
            pManager.AddNumberParameter ("Adaptivity", "A", "Value range from 0-1. Higher adaptivities will allow more variation " +
                "in polygon size, resulting in fewer polygons.", GH_ParamAccess.item, 0.1);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams (GH_Component.GH_OutputParamManager pManager) {
            pManager.AddGenericParameter ("Volume Settings", "S", "Global Volume Settings to be used", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance (IGH_DataAccess DA) {
            double bandwidth = 5.0;
            double voxelSize = 1.0;
            double isoValue = 0.0;
            double adaptivity = 0.1;

            if (!DA.GetData (0, ref voxelSize)) return;
            if (!DA.GetData (1, ref bandwidth)) return;
            if (!DA.GetData (2, ref isoValue)) return;
            if (!DA.GetData (3, ref adaptivity)) return;

            DendroSettings vs = new DendroSettings ();

            vs.Adaptivity = adaptivity;
            vs.Bandwidth = bandwidth;
            vs.IsoValue = isoValue;
            vs.VoxelSize = voxelSize;

            DA.SetData (0, new SettingsGOO (vs));
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon {
            get {
                return DendroGH.Properties.Resources.ico_settings;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid {
            get { return new Guid ("6db84a8d-28e4-4ebe-bf2f-c954caf319b9"); }
        }
    }
}