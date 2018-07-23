using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace DendroGH {
    public class PointsToVolume : GH_Component {
        /// <summary>
        /// Initializes a new instance of the PointsToVolume class.
        /// </summary>
        public PointsToVolume () : base ("Points To Volume", "vPoints",
            "Create a volume from a point set",
            "Dendro", "Convert") { }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams (GH_Component.GH_InputParamManager pManager) {
            pManager.AddPointParameter ("Points", "P", "Points to convert into a volume", GH_ParamAccess.list);
            pManager.AddNumberParameter ("Point Radius", "R", "Supply one value or a list of values equal to the number of curves supplied", GH_ParamAccess.list);
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
            List<Point3d> vPoints = new List<Point3d> ();
            List<double> vRadius = new List<double> ();
            DendroSettings vSettings = new DendroSettings ();

            if (!DA.GetDataList (0, vPoints)) return;
            if (!DA.GetDataList (1, vRadius)) return;
            if (!DA.GetData (2, ref vSettings)) return;

            double minRadius = vSettings.VoxelSize / 0.6667;

            foreach (double radius in vRadius)
            {
                if (radius <= minRadius)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Radius must be at least 33% larger than voxel size. This will compute but no volume will be created.");
                }
            }

            DendroVolume volume = new DendroVolume (vPoints, vRadius, vSettings);

            if (!volume.IsValid) {
                AddRuntimeMessage (GH_RuntimeMessageLevel.Error, "Conversion failed. Make sure you supplied valid radius values or valid settings");
                return;
            }

            DA.SetData (0, new VolumeGOO (volume));
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon {
            get {
                return DendroGH.Properties.Resources.ico_point_vox;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid {
            get { return new Guid ("fe8d2a29-8f2b-4952-ad70-bf8d3c23427a"); }
        }
    }
}