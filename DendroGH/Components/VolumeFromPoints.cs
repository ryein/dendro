using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace DendroGH
{
    public class PointsToVolume : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the PointsToVolume class.
        /// </summary>
        public PointsToVolume() : base("Points To Volume", "vPoints",
            "Create a volume from a point set",
            "Dendro", "Create")
        { }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "P", "Points to convert into a volume", GH_ParamAccess.list);
            pManager.AddNumberParameter("Point Radius", "R", "Supply one radius for all points or one radius per supplied point", GH_ParamAccess.list);
            pManager.AddGenericParameter("Settings", "S", "Settings for converting different geometry types to and from volumes", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Volume", "V", "Volume geometry", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Point3d> vPoints = new List<Point3d>();
            List<double> vRadius = new List<double>();
            DendroSettings vSettings = new DendroSettings();

            if (!DA.GetDataList(0, vPoints)) return;
            if (!DA.GetDataList(1, vRadius)) return;
            if (!DA.GetData(2, ref vSettings)) return;

            if (vSettings == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Volume settings are required.");
                return;
            }

            DendroVolume volume = new DendroVolume(vPoints, vRadius, vSettings);

            if (volume.SkippedPointIndices.Count > 0)
            {
                const int displayedIndexLimit = 20;
                string displayedIndices = string.Join(
                    ", ",
                    volume.SkippedPointIndices.Take(displayedIndexLimit));
                string remainder = volume.SkippedPointIndices.Count > displayedIndexLimit
                    ? $" (+{volume.SkippedPointIndices.Count - displayedIndexLimit} more)"
                    : string.Empty;
                AddRuntimeMessage(
                    GH_RuntimeMessageLevel.Warning,
                    $"Skipped points below the 1.5-voxel minimum radius at zero-based indices: {displayedIndices}{remainder}.");
            }

            if (!volume.IsValid)
            {
                string error = string.IsNullOrWhiteSpace(volume.ErrorMessage)
                    ? "Conversion failed. Make sure you supplied valid points, radii, and settings."
                    : volume.ErrorMessage;
                volume.Dispose();
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, error);
                return;
            }
            DA.SetData(0, new VolumeGOO(volume));
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return DendroGH.Properties.Resources.ico_point_vox;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("fe8d2a29-8f2b-4952-ad70-bf8d3c23427a"); }
        }
    }
}
