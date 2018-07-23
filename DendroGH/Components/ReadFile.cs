using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace DendroGH {
    public class ReadFile : GH_Component {
        /// <summary>
        /// Initializes a new instance of the ReadFile class.
        /// </summary>
        public ReadFile () : base ("Read Volume", "vRead",
            "Read a volume file and create a volume",
            "Dendro", "IO") { }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams (GH_Component.GH_InputParamManager pManager) {
            pManager.AddTextParameter ("File Path", "F", "Path of volume file (*.vdb files)", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Read?", "R", "Boolean toggle to read file", GH_ParamAccess.item);

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
            string filepath = "";
            bool isRead = false;

            if (!DA.GetData (0, ref filepath)) return;
            if (!DA.GetData(1, ref isRead)) return;

            DendroVolume volume = new DendroVolume();

            if (isRead)
            {
                volume = new DendroVolume(filepath);

                if (!volume.IsValid)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Read failed. Make sure you supplied valid *.VDB file. If file wasn't output from Dendro, make sure your source outputs volume as a float grid");
                    return;
                }
            }

            DA.SetData (0, new VolumeGOO (volume));

        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon {
            get {
                return DendroGH.Properties.Resources.ico_io_in;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid {
            get { return new Guid ("053635d3-457b-4833-9560-128f2501de74"); }
        }
    }
}