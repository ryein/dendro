using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace DendroGH.Components
{
    public class ClosestPoint : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ClosestPoint class.
        /// </summary>
        public ClosestPoint()
          : base("Volume Closest Point", "vCP",
              "Find the closest point to a volume from a supplied list of points",
            "Dendro", "Analysis")

        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Volume", "V", "Volume geometry", GH_ParamAccess.item);
            pManager.AddPointParameter("Points", "P", "Points to solve for", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Closest Points", "CP", "Closest Points on the volume", GH_ParamAccess.list);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DendroVolume volume = new DendroVolume();
            List<Point3d> vPoints = new List<Point3d>();

            if (!DA.GetData(0, ref volume)) return;
            if (!DA.GetDataList(1, vPoints)) return;

            List<Point3d> cp = volume.ClosestPoint(vPoints);

            DA.SetDataList(0, cp);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return DendroGH.Properties.Resources.ico_cp;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("1aace784-5b00-481c-ad49-fa8eaa10a662"); }
        }
    }
}