using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace DendroGH
{
    public class CurveToVolume : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the CurveToVolume class.
        /// </summary>
        public CurveToVolume() : base("Curve To Volume", "vCurve",
            "Create a volume from a list of curves",
            "Dendro", "Convert")
        { }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves", "C", "Curves", GH_ParamAccess.list);
            pManager.AddNumberParameter(
                "Curve Radius",
                "R",
                "Supply one radius for all curves or one radius per supplied curve",
                GH_ParamAccess.list);
            pManager.AddGenericParameter("Settings", "S", "Settings for converting different geometry types to and from volumes", GH_ParamAccess.item);
            pManager.AddNumberParameter(
                "Curve Deviation",
                "D",
                "Maximum world-space deviation between input curves and their polyline approximation. Use 0 for automatic.",
                GH_ParamAccess.item,
                0.0);
            pManager[3].Optional = true;
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
            List<Curve> vCurves = new List<Curve>();
            List<double> vRadius = new List<double>();
            DendroSettings vSettings = new DendroSettings();
            double curveDeviation = 0.0;

            if (!DA.GetDataList(0, vCurves)) return;
            if (!DA.GetDataList(1, vRadius)) return;
            if (!DA.GetData(2, ref vSettings)) return;
            DA.GetData(3, ref curveDeviation);

            if (vSettings == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Volume settings are required.");
                return;
            }

            if (curveDeviation < 0.0 || double.IsNaN(curveDeviation) || double.IsInfinity(curveDeviation))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Curve deviation must be zero (automatic) or a positive finite value.");
                return;
            }

            var invalidCurveIndices = vCurves
                .Select((curve, index) => new { curve, index })
                .Where(item => item.curve == null || !item.curve.IsValid)
                .Select(item => item.index)
                .ToList();

            if (invalidCurveIndices.Count > 0)
            {
                const int displayedIndexLimit = 20;
                string displayedIndices = string.Join(", ", invalidCurveIndices.Take(displayedIndexLimit));
                string remainder = invalidCurveIndices.Count > displayedIndexLimit
                    ? $" (+{invalidCurveIndices.Count - displayedIndexLimit} more)"
                    : string.Empty;
                AddRuntimeMessage(
                    GH_RuntimeMessageLevel.Warning,
                    $"Skipped invalid curves at zero-based indices: {displayedIndices}{remainder}.");
            }

            double voxelSize = vSettings.VoxelSize;
            if (voxelSize > 0.0 && !double.IsNaN(voxelSize) && !double.IsInfinity(voxelSize) &&
                curveDeviation > 0.0 && curveDeviation < voxelSize * 0.1)
            {
                AddRuntimeMessage(
                    GH_RuntimeMessageLevel.Warning,
                    "Curve deviation is less than 10% of the voxel size. This may add substantial computation without visible voxel-level detail.");
            }

            DendroVolume volume = new DendroVolume(vCurves, vRadius, vSettings, curveDeviation);

            if (!volume.IsValid)
            {
                string error = string.IsNullOrWhiteSpace(volume.ErrorMessage)
                    ? "Conversion failed. Make sure you supplied valid curves, radius, and settings."
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
                return DendroGH.Properties.Resources.ico_curve_vox;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("5d75d6c9-f24d-4f1e-a77c-984ea78dd5b0"); }
        }
    }
}
