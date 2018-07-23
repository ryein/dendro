using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Rhino.Geometry;

namespace DendroGH {
    /// <summary>
    /// c# wrapper for c++ api. this holds all external dll calls and is 
    /// primary point of communication with all openvdb functions. the mGrid
    /// member is a pointer to a c++ class and all DendroVolume methods operate
    /// specific functions on that c++ mGrid class
    /// </summary>
    public class DendroVolume : IDisposable {

#region PInvokes
        [DllImport ("DendroAPI.dll", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr DendroCreate ();

        [DllImport ("DendroAPI.dll", CallingConvention = CallingConvention.Cdecl)]
        static private extern void DendroDelete (IntPtr grid);

        [DllImport ("DendroAPI.dll", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr DendroDuplicate (IntPtr grid);

        [DllImport ("DendroAPI.dll", CallingConvention = CallingConvention.Cdecl)]
        static private extern bool DendroRead (IntPtr grid, string filename);

        [DllImport ("DendroAPI.dll", CallingConvention = CallingConvention.Cdecl)]
        static private extern bool DendroWrite (IntPtr grid, string filename);

        [DllImport ("DendroAPI.dll", CallingConvention = CallingConvention.Cdecl)]
        static private extern bool DendroFromMesh(IntPtr grid, float[] vertices, int vCount, int[] faces, int fCount, double voxelSize, double bandwidth);

        [DllImport ("DendroAPI.dll", CallingConvention = CallingConvention.Cdecl)]
        static private extern bool DendroFromPoints(IntPtr grid, double[] points, int pCount, double[] radii, int rCount, double voxelSize, double bandwidth);

        [DllImport ("DendroAPI.dll", CallingConvention = CallingConvention.Cdecl)]
        static private extern bool DendroTransform (IntPtr grid, double[] matrix, int mCount);

        [DllImport ("DendroAPI.dll", CallingConvention = CallingConvention.Cdecl)]
        static public extern void DendroUnion (IntPtr grid, IntPtr csgGrid);

        [DllImport ("DendroAPI.dll", CallingConvention = CallingConvention.Cdecl)]
        static public extern void DendroDifference (IntPtr grid, IntPtr csgGrid);

        [DllImport ("DendroAPI.dll", CallingConvention = CallingConvention.Cdecl)]
        static public extern void DendroIntersection (IntPtr grid, IntPtr csgGrid);

        [DllImport ("DendroAPI.dll", CallingConvention = CallingConvention.Cdecl)]
        static public extern void DendroOffset (IntPtr grid, double amount);

        [DllImport ("DendroAPI.dll", CallingConvention = CallingConvention.Cdecl)]
        static public extern void DendroOffsetMask (IntPtr grid, double amount, IntPtr mask, double min, double max, bool invert);

        [DllImport ("DendroAPI.dll", CallingConvention = CallingConvention.Cdecl)]
        static public extern void DendroSmooth (IntPtr grid, int type, int iterations, int width);

        [DllImport ("DendroAPI.dll", CallingConvention = CallingConvention.Cdecl)]
        static public extern void DendroSmoothMask (IntPtr grid, int type, int iterations, int width, IntPtr mask, double min, double max, bool invert);

        [DllImport ("DendroAPI.dll", CallingConvention = CallingConvention.Cdecl)]
        static public extern void DendroBlend (IntPtr bGrid, IntPtr eGrid, double bPosition, double bEnd);

        [DllImport ("DendroAPI.dll", CallingConvention = CallingConvention.Cdecl)]
        static public extern void DendroBlendMask (IntPtr bGrid, IntPtr eGrid, double bPosition, double bEnd, IntPtr mask, double min, double max, bool invert);

        [DllImport ("DendroAPI.dll", CallingConvention = CallingConvention.Cdecl)]
        static private extern void DendroToMesh (IntPtr grid);

        [DllImport ("DendroAPI.dll", CallingConvention = CallingConvention.Cdecl)]
        static private extern void DendroToMeshSettings (IntPtr grid, double isovalue, double adaptivity);

        [DllImport ("DendroAPI.dll", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr DendroVertexBuffer (IntPtr grid, out int size);

        [DllImport ("DendroAPI.dll", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr DendroFaceBuffer (IntPtr grid, out int size);
#endregion PInvokes

#region Members
        private IntPtr mGrid; // stores pointer to grid in c++
        private Mesh mDisplay; // mesh representation of volume used for visualization
        private bool mValid; // volume validity
#endregion Members

#region Constructors
        /// <summary>
        /// default constructor
        /// </summary>
        public DendroVolume () {
            // pinvoke grid creation
            this.Grid = DendroCreate ();
            this.Display = new Mesh ();

            // volume is empty, so it is invalid
            this.IsValid = false;
        }

        /// <summary>
        /// copy constructor
        /// </summary>
        /// <param name="vCopy">volume to copy from</param>
        public DendroVolume (DendroVolume vCopy) {
            if (vCopy.IsValid) {
                this.Grid = vCopy.DuplicateGrid ();
                this.Display = vCopy.Display.DuplicateMesh ();

                this.IsValid = true;
            }
            else {
                // pinvoke grid creation
                this.Grid = DendroCreate ();
                this.Display = new Mesh ();

                // volume is empty, so it is invalid
                this.IsValid = false;
            }
        }

        /// <summary>
        /// create volume from a vdb file
        /// </summary>
        /// <param name="vFile">full path and name of vdb file to read (vdb extension)</param>
        public DendroVolume (string vFile) {
            // create a new grid
            this.Grid = DendroCreate ();
            this.Display = new Mesh ();

            // read vdb file
            this.IsValid = this.Read (vFile);
        }

        /// <summary>
        /// mesh constructor 
        /// </summary>
        /// <param name="vMesh">mesh to build volume from</param>
        /// <param name="vSettings">voxelization settings to be used</param>
        public DendroVolume (Mesh vMesh, DendroSettings vSettings) {
            // pinvoke grid creation
            this.Grid = DendroCreate ();
            this.Display = new Mesh ();

            this.IsValid = this.CreateFromMesh (vMesh, vSettings);
        }

        /// <summary>
        /// point constructor
        /// </summary>
        /// <remark>must supply a single radius value or a list of radii equal to the number of points supplied</remark>
        /// <param name="vPoints">point set to build volume from</param>
        /// <param name="vRadius">radius values for each point</param>
        /// <param name="vSettings">voxelization settings to be used</param>
        public DendroVolume (List<Point3d> vPoints, List<double> vRadius, DendroSettings vSettings) {
            // pinvoke grid creation
            this.Grid = DendroCreate ();
            this.Display = new Mesh ();

            this.IsValid = this.CreateFromPoints (vPoints, vRadius, vSettings);
        }

        /// <summary>
        /// curve constructor
        /// </summary>
        /// <remark>must supply a single radius value or a list of radii equal to the number of curves supplied</remark>
        /// <param name="vCurves">curves to build volume from</param>
        /// <param name="vRadius">radius values for each curve</param>
        /// <param name="vSettings">voxelization settings to be used</param>
        public DendroVolume (List<Curve> vCurves, List<double> vRadius, DendroSettings vSettings) {
            // pinvoke grid creation
            this.Grid = DendroCreate ();
            this.Display = new Mesh ();

            this.IsValid = this.CreateFromCurves (vCurves, vRadius, vSettings);
        }

        /// <summary>
        /// dispose of volume and release resources
        /// </summary>
        public void Dispose () {
            Dispose (true);
        }

        /// <summary>
        /// protected implementation of dispose pattern
        /// </summary>
        /// <param name="bDisposing">holds value indicating if this was called from dispose or finalizer</param>
        protected virtual void Dispose (bool bDisposing) {
                if (this.Grid != IntPtr.Zero) {
                    // cleanup everything on the c++ side
                    DendroDelete (this.Grid);

                    // clear grid pointer
                    this.Grid = IntPtr.Zero;
                    this.IsValid = false;
                }

                // finalize garbage collection
                if (bDisposing) {
                    GC.SuppressFinalize (this);
                }
            }

            /// <summary>
            /// destructor
            /// </summary>
            ~DendroVolume () {
                Dispose (false);
            }

        /// <summary>
        /// duplicate the volume grid
        /// </summary>
        /// <remarks>
        /// needed in addition to the copy contructor in order to duplicate just the c++ grid class
        /// </remarks>
        /// <returns>returns pointer to c++ grid</returns>
        public IntPtr DuplicateGrid () {
            // pinvoke grid duplication
            return DendroDuplicate (this.Grid);
        }
#endregion Constructors

#region Properties
        /// <summary>
        /// volume validity property
        /// </summary>
        /// <returns>boolean value of volume validity</returns>
        public bool IsValid {
            get {
                if (this.Grid == IntPtr.Zero) return false;
                return this.mValid;
            }
            private set {
                this.mValid = value;
            }
        }

        /// <summary>
        /// volume grid pointer property
        /// </summary>
        /// <returns>pointer to c++ grid</returns>
        public IntPtr Grid {
            get {
                return this.mGrid;
            }
            private set {
                this.mGrid = value;
            }
        }

        /// <summary>
        /// volume mesh represention for visualization
        /// </summary>
        /// <returns>mesh representation of volume</returns>
        public Mesh Display {
            get {
                return this.mDisplay;
            }
            private set {
                this.mDisplay = value;
            }
        }
#endregion Properties

        #region Methods
        /// <summary>
        /// read a vdb file and build volume
        /// </summary>
        /// <param name="vFile">full path and name of vdb file to read (vdb extension)</param>
        /// <returns>boolean value for whether reading was successful</returns>
        public bool Read (string vFile) {
            if (!File.Exists (vFile)) {
                return false;
            }

            // pinvoke file read function
            this.IsValid = DendroRead (this.Grid, vFile);

            if (!this.IsValid) {
                return false;
            }

            this.UpdateDisplay ();

            return true;
        }

        /// <summary>
        /// write volume to a vdb file
        /// </summary>
        /// <param name="vFile">full path and name of vdb file to write (vdb extension)</param>
        /// <returns>boolean value for whether file write was successful</returns>
        public bool Write (string vFile) {
            // pinvoke file writing function
            DendroWrite (this.Grid, vFile);

            if (!File.Exists (vFile)) {
                return false;
            }

            return true;
        }

        /// <summary>
        /// build a volume from a mesh input
        /// </summary>
        /// <param name="vMesh">mesh to build volume from</param>
        /// <param name="vSettings">voxelization settings to be used</param>
        /// <returns>boolean value for whether volume was built successfully</returns>
        public bool CreateFromMesh (Mesh vMesh, DendroSettings vSettings) {
            if (!vMesh.IsValid)
                return false;

            // check for invalid voxelsize settings
            if (vSettings.VoxelSize < 0.01)
                vSettings.VoxelSize = 0.01;

            // check for invalid bandwidth settings
            if (vSettings.Bandwidth < 1)
                vSettings.Bandwidth = 1;

            // create flattened vertex and face arrays from input mesh
            float[] vertices = vMesh.Vertices.ToFloatArray ();
            int[] faces = vMesh.Faces.ToIntArray (true);

            // pinvoke build volume from mesh
            this.IsValid = DendroFromMesh(this.Grid, vertices, vertices.Length, faces, faces.Length, vSettings.VoxelSize, vSettings.Bandwidth);

            if (!this.IsValid)
                return false;

            // use input mesh for the visualization mesh (saves us from computing it in c++)
            this.Display = vMesh.DuplicateMesh ();

            return true;
        }

        /// <summary>
        /// build a volume from a supplied list of points
        /// </summary>
        /// <remark>must supply a single radius value or a list of radii equal to the number of points supplied</remark>
        /// <param name="vPoints">point set to build volume from</param>
        /// <param name="vRadius">radius values for each point</param>
        /// <param name="vSettings">voxelization settings to be used</param>
        /// <returns>boolean value for whether volume was built successfully</returns>
        public bool CreateFromPoints (List<Point3d> vPoints, List<double> vRadius, DendroSettings vSettings) {
            // there were no points/radius supplied so exit
            if (vPoints.Count == 0 || vRadius.Count == 0)
                return false;

            // check for invalid voxelsize settings
            if (vSettings.VoxelSize < 0.01)
                vSettings.VoxelSize = 0.01;

            // check for invalid bandwidth settings
            if (vSettings.Bandwidth < 1)
                vSettings.Bandwidth = 1;

            // if one or equal radius values were supplied then build volume
            if (vPoints.Count == vRadius.Count || vRadius.Count == 1) {

                // create point array from point3d list so we can pass to c++
                double[] points = new double[vPoints.Count * 3];

                int i = 0;
                foreach (Point3d pt in vPoints) {
                    points[i] = pt.X;
                    points[i + 1] = pt.Y;
                    points[i + 2] = pt.Z;

                    i += 3;
                }

                // create radius array from list so we can pass to c++
                double[] radius = vRadius.ToArray ();

                // pinvoke build volume from points
                this.IsValid = DendroFromPoints(this.Grid, points, points.Length, radius, radius.Length, vSettings.VoxelSize, vSettings.Bandwidth);

                if (!this.IsValid)
                    return false;

                this.UpdateDisplay ();
            }
            else {
                return false;
            }

            return true;
        }

        /// <summary>
        /// build a volume from a supplied list of curves
        /// </summary>
        /// <remark>must supply a single radius value or a list of radii equal to the number of curves supplied</remark>
        /// <param name="vCurves">curves to build volume from</param>
        /// <param name="vRadius">radius values for each curve</param>
        /// <param name="vSettings">voxelization settings to be used</param>
        /// <returns>boolean value for whether volume was built successfully</returns>
        public bool CreateFromCurves (List<Curve> vCurves, List<double> vRadius, DendroSettings vSettings) {
            // there were no curves/radius supplied so exit
            if (vCurves.Count == 0 || vRadius.Count == 0)
                return false;

            // check for invalid voxelsize settings
            if (vSettings.VoxelSize < 0.01)
                vSettings.VoxelSize = 0.01;

            // check for invalid bandwidth settings
            if (vSettings.Bandwidth < 1)
                vSettings.Bandwidth = 1;

            // find out if we were supplied a single radius value or multiple values
            int method = GetCurveSolverMethod (vCurves.Count, vRadius.Count);

            bool validInput = false;
            List<double> rValues = new List<double> ();
            List<Point3d> vPoints = new List<Point3d> ();

            switch (method) {
                // only a single radius was supplied
                case 1:
                    validInput = ResolveSingleRadius (vCurves, vRadius[0], out vPoints, out rValues);
                    break;

                    // multiple radius values were supplied
                case 2:
                    validInput = ResolveMultipleRadius (vCurves, vRadius, out vPoints, out rValues);
                    break;
                default:
                    validInput = false;
                    break;
            }

            // supplied values were not valid so exit
            if (!validInput)
                return false;

            // return results from point to volume function
            return this.CreateFromPoints (vPoints, rValues, vSettings);
        }

        /// <summary>
        /// gets the world axis aligned boundingbox for the volume
        /// </summary>
        /// <returns>boundingbox of the geometry in world coordinates or BoundingBox.Empty if not bounding box could be found</returns>
        public BoundingBox GetBoundingBox () {
            return this.Display.GetBoundingBox (true);
        }

        /// <summary>
        /// gets the world axis aligned boundingbox for the transformed volume
        /// </summary>
        /// <param name="xform">transformation to apply to object prior to the bounding box computation</param>
        /// <returns>accurate boundingbox of the transformed geometry in world coordinates or BoundingBox.Empty if not bounding box could be found</returns>
        public BoundingBox GetBoundingBox (Transform xform) {
            return this.Display.GetBoundingBox (xform);
        }

        /// <summary>
        /// transform volume from supplied matrix
        /// </summary>
        /// <param name="xform">transform to apply to volume</param>
        /// <returns>boolean value for whether transform was successful</returns>
        public bool Transform (Transform xform) {
            if (!xform.IsValid)
                return false;

            // convert transform to flat array
            float[] floatMatrix = xform.ToFloatArray (false);
            double[] matrix = Array.ConvertAll (floatMatrix, x => (double) x);

            // pinvoke transform on grid
            this.IsValid = DendroTransform (this.Grid, matrix, matrix.Length);

            if (!this.IsValid)
                return false;

            // transform display mesh directly instead of recomputing in c++
            this.Display.Transform (xform);

            return true;
        }

        /// <summary>
        /// compute a boolean difference of a volume
        /// </summary>
        /// <param name="vSubract">volume to subtract with</param>
        /// <returns>new volume with the resulting difference</returns>
        public DendroVolume BooleanDifference (DendroVolume vSubract) {
            if (!this.IsValid)
                return new DendroVolume ();

            if (!vSubract.IsValid)
                return new DendroVolume (this);

            DendroVolume csg = new DendroVolume (this);

            // pinvoke difference function
            DendroDifference (csg.Grid, vSubract.Grid);
            csg.UpdateDisplay ();

            return csg;
        }

        /// <summary>
        /// compute a boolean difference of a set of volumes
        /// </summary>
        /// <param name="vSubtract">list of volumes to subtract with</param>
        /// <returns>new volume with the resulting difference</returns>
        public DendroVolume BooleanDifference (List<DendroVolume> vSubtract) {
            if (!this.IsValid)
                return new DendroVolume ();

            DendroVolume csg = new DendroVolume (this);

            foreach (DendroVolume subtract in vSubtract) {
                // pinvoke difference function
                if (subtract.IsValid)
                    DendroDifference (csg.Grid, subtract.Grid);
            }

            csg.UpdateDisplay ();

            return csg;
        }

        /// <summary>
        /// compute a boolean intersection of a volume
        /// </summary>
        /// <param name="vIntersect">volume to intersect</param>
        /// <returns>new volume with the resulting intersection</returns>
        public DendroVolume BooleanIntersection (DendroVolume vIntersect) {
            if (!this.IsValid)
                return new DendroVolume ();

            if (!vIntersect.IsValid)
                return new DendroVolume (this);

            DendroVolume csg = new DendroVolume (this);

            // pinvoke intersection function
            DendroIntersection (csg.Grid, vIntersect.Grid);
            csg.UpdateDisplay ();

            return csg;
        }

        /// <summary>
        /// compute a boolean intersection of a set of volumes
        /// </summary>
        /// <param name="vIntersect">list of volumes to intersect</param>
        /// <returns>new volume with the resulting intersection</returns>
        public DendroVolume BooleanIntersection (List<DendroVolume> vIntersect) {
            if (!this.IsValid)
                return new DendroVolume ();

            DendroVolume csg = new DendroVolume (this);

            foreach (DendroVolume intersect in vIntersect) {
                // pinvoke intersection function
                if (intersect.IsValid)
                    DendroIntersection (csg.Grid, intersect.Grid);
            }

            csg.UpdateDisplay ();

            return csg;
        }

        /// <summary>
        /// compute a boolean union of a volume
        /// </summary>
        /// <param name="vUnion">volume to union</param>
        /// <returns>new volume with the resulting union</returns>
        public DendroVolume BooleanUnion (DendroVolume vUnion) {
            if (!this.IsValid)
                return new DendroVolume ();

            if (!vUnion.IsValid)
                return new DendroVolume (this);

            DendroVolume csg = new DendroVolume (this);

            // pinvoke union function
            DendroUnion (csg.Grid, vUnion.Grid);
            csg.UpdateDisplay ();

            return csg;
        }

        /// <summary>
        /// compute a boolean union of a set of volumes
        /// </summary>
        /// <param name="vUnion">list of volumes to union</param>
        /// <returns>new volume with the resulting union</returns>
        public DendroVolume BooleanUnion (List<DendroVolume> vUnion) {
            if (!this.IsValid)
                return new DendroVolume ();

            DendroVolume csg = new DendroVolume (this);

            foreach (DendroVolume union in vUnion) {
                // pinvoke union function
                if (union.IsValid)
                    DendroUnion (csg.Grid, union.Grid);
            }

            csg.UpdateDisplay ();

            return csg;
        }

        /// <summary>
        /// apply an offset to the volume
        /// </summary>
        /// <param name="amount">amount to offset volume</param>
        /// <returns>offset volume</returns>
        public DendroVolume Offset (double amount) {
            if (!this.IsValid)
                return new DendroVolume ();

            DendroVolume offset = new DendroVolume (this);

            // pinvoke offset function
            DendroOffset (offset.Grid, amount);

            offset.UpdateDisplay ();

            return offset;
        }

        /// <summary>
        /// apply an offset to the volume with a mask
        /// </summary>
        /// <param name="amount">amount to offset volume</param>
        /// <param name="vMask">mask for offset operation</param>
        /// <returns>offset volume</returns>
        public DendroVolume Offset (double amount, DendroMask vMask) {
            if (!this.IsValid)
                return new DendroVolume ();

            DendroVolume offset = new DendroVolume (this);

            // pinvoke offset function with mask
            DendroOffsetMask (offset.Grid, amount, vMask.Volume.Grid, vMask.Min, vMask.Max, vMask.Invert);

            offset.UpdateDisplay ();

            return offset;
        }

        /// <summary>
        /// apply smoothing to a volume
        /// </summary>
        /// <param name="sWidth">(optional) width of the mean-value filter is 2*width+1 voxels</param>
        /// <param name="sType">0 - gaussian, 1 - laplacian, 2 - mean, 3 - median</param>
        /// <param name="sIterations">number of smoothing operations to perform</param>
        /// <returns>smoothed volume</returns>
        public DendroVolume Smooth (int sType, int sIterations, int sWidth = 1)
        {
            if (!this.IsValid)
                return new DendroVolume ();

            if (sType < 0 || sType > 3)
                sType = 1;

            if (sWidth < 1)
                sWidth = 1;

            if (sIterations < 1)
                sIterations = 1;

            DendroVolume smooth = new DendroVolume(this);

            // pinvoke smoothing function
            DendroSmooth (smooth.Grid, sType, sIterations, sWidth);

            smooth.UpdateDisplay ();

            return smooth;
        }

        /// <summary>
        /// apply smoothing to a volume
        /// </summary>
        /// <param name="sWidth">(optional) width of the mean-value filter is 2*width+1 voxels</param>
        /// <param name="sType">0 - gaussian, 1 - laplacian, 2 - mean, 3 - median</param>
        /// <param name="sIterations">number of smoothing operations to perform</param>
        /// <param name="vMask">mask for smoothing operation</param>
        /// <returns>smoothed volume</returns>
        public DendroVolume Smooth (int sType, int sIterations, DendroMask vMask, int sWidth = 1) {
            if (!this.IsValid)
                return new DendroVolume ();

            if (sType < 0 || sType > 3)
                sType = 1;

            if (sWidth < 1)
                sWidth = 1;

            if (sIterations < 1)
                sIterations = 1;

            DendroVolume smooth = new DendroVolume (this);

            // pinvoke smoothing function with mask
            DendroSmoothMask (smooth.Grid, sType, sIterations, sWidth, vMask.Volume.Grid, vMask.Min, vMask.Max, vMask.Invert);

            smooth.UpdateDisplay ();

            return smooth;
        }

        /// <summary>
        /// blend two volumes
        /// </summary>
        /// <param name="bVolume">volume to blend with</param>
        /// <param name="bPosition">position parameter to sample blending at (normalized 0-1)</param>
        /// <returns>blended volume</returns>
        public DendroVolume Blend (DendroVolume bVolume, double bPosition, double bEnd) {
            if (!this.IsValid)
                return new DendroVolume ();

            if (bPosition < 0) bPosition = 0;
            if (bPosition > 1) bPosition = 1;

            if (bEnd < 1) bEnd = 1;

            bPosition = 1 - bPosition;

            DendroVolume blend = new DendroVolume(this);

            // pinvoke smoothing function
            DendroBlend(blend.Grid, bVolume.Grid, bPosition, bEnd);

            blend.UpdateDisplay ();

            return blend;
        }

        /// <summary>
        /// blend two volumes using a mask
        /// </summary>
        /// <param name="bVolume">volume to blend with</param>
        /// <param name="bPosition">position parameter to sample blending at (normalized 0-1)</param>
        /// <param name="vMask">mask for blending operation</param>
        /// <returns>blended volume</returns>
        public DendroVolume Blend (DendroVolume bVolume, double bPosition, double bEnd, DendroMask vMask) {
            if (!this.IsValid)
                return new DendroVolume ();

            if (bPosition < 0) bPosition = 0;
            if (bPosition > 1) bPosition = 1;

            if (bEnd < 1) bEnd = 1;

            bPosition = 1 - bPosition;

            DendroVolume blend = new DendroVolume (this);

            // pinvoke smoothing function with mask
            DendroBlendMask (blend.Grid, bVolume.Grid, bPosition, bEnd, vMask.Volume.Grid, vMask.Min, vMask.Max, vMask.Invert);

            blend.UpdateDisplay ();

            return blend;
        }
#endregion Methods

#region Display
        /// <summary>
        /// update the mesh representation of the volume
        /// </summary>
        public void UpdateDisplay () {
            // pinvoke mesh update
            DendroToMesh (this.Grid);

            // get update vertex and face arrays
            float[] vertices = this.GetMeshVertices ();
            int[] faces = this.GetMeshFaces ();

            this.Display = this.ConstructMesh (vertices, faces);

            // flip and rebuild
            this.Display.Normals.ComputeNormals();
        }

        /// <summary>
        /// update the mesh representation of the volume using voxel settings
        /// </summary>
        /// <param name="vSettings">settings for meshing volume</param>
        public void UpdateDisplay (DendroSettings vSettings) {
            // pinvoke mesh update
            DendroToMeshSettings (this.Grid, vSettings.IsoValue, vSettings.Adaptivity);

            // get update vertex and face arrays
            float[] vertices = this.GetMeshVertices ();
            int[] faces = this.GetMeshFaces ();

            this.Display = this.ConstructMesh (vertices, faces);

            // flip and rebuild
            this.Display.UnifyNormals();
            this.Display.Flip(true, true, true);
        }

        /// <summary>
        /// gets vertex buffer array over from c++
        /// </summary>
        /// <returns>vertex array</returns>
        private float[] GetMeshVertices () {
            float[] result = null;

            IntPtr vertices = IntPtr.Zero;

            // pinvoke vertex buffer
            vertices = DendroVertexBuffer (this.Grid, out int size);
            if (vertices != IntPtr.Zero) {
                result = new float[size];
                Marshal.Copy (vertices, result, 0, size);
            }

            Marshal.FreeHGlobal (vertices);

            return result;
        }

        /// <summary>
        /// gets face buffer array over from c++
        /// </summary>
        /// <returns>face array</returns>
        private int[] GetMeshFaces () {
            int[] result = null;

            IntPtr faces = IntPtr.Zero;

            // pinvoke face buffer
            faces = DendroFaceBuffer (this.Grid, out int size);
            if (faces != IntPtr.Zero) {
                result = new int[size];
                Marshal.Copy (faces, result, 0, size);
            }

            Marshal.FreeHGlobal (faces);

            return result;
        }

        /// <summary>
        /// build mesh from vertex and face array
        /// </summary>
        /// <param name="vertices">vertex array for mesh construction</param>
        /// <param name="faces">face array for mesh construction</param>
        /// <returns>constructed mesh</returns>
        private Mesh ConstructMesh (float[] vertices, int[] faces) {
            Mesh constructed = new Mesh ();

            // add vertices to mesh
            int i = 0;
            while (i < vertices.Length) {
                float x = vertices[i];
                float y = vertices[i + 1];
                float z = vertices[i + 2];

                constructed.Vertices.Add (x, y, z);

                i += 3;
            }

            // add faces to mesh
            i = 0;
            while (i < faces.Length) {
                int w = faces[i];
                int x = faces[i + 1];
                int y = faces[i + 2];
                int z = faces[i + 3];

                if (w == -1) {
                    constructed.Faces.AddFace (x, y, z);
                }
                else {
                    constructed.Faces.AddFace (w, x, y, z);
                }
                i += 4;
            }

            return constructed;
        }

#endregion Display

#region Helpers
        /// <summary>
        /// create a point set, with a corresponding radius value list, for every curve. each curve provided 
        /// is divided into points, using its supplied radius value and then added to the whole point set.
        /// </summary>
        /// <remark>called from CreateFromCurve when multiple radius values are supplied</remark>
        /// <param name="vCurves">curves to divide into points</param>
        /// <param name="cRadius">desired radius value for the points of each curve</param>
        /// <param name="vPoints">list to store all points for every curve</param>
        /// <param name="vRadius">list to store all radius values for every point in vPoints</param>
        /// <returns>boolean with whether operation was successful</returns>
        private bool ResolveMultipleRadius (List<Curve> vCurves, List<double> cRadius, out List<Point3d> vPoints, out List<double> vRadius) {
            vRadius = new List<double> ();
            vPoints = new List<Point3d> ();

            int rIndex = 0;

            foreach (Curve crv in vCurves) {
                var radius = cRadius[rIndex];

                if (radius > 0) {

                    List<Point3d> vp = this.CurveToPoints (crv, radius);
                    List<double> rv = Enumerable.Repeat (radius, vp.Count).ToList ();
                    vPoints.AddRange (vp);
                    vRadius.AddRange (rv);
                }
                else {
                    return false;
                }

                rIndex++;
            }

            return true;
        }

        /// <summary>
        /// create a point set, with a corresponding radius value list, for every curve. each curve provided 
        /// is divided into points, using its supplied radius value and then added to the whole point set.
        /// </summary>
        /// <remark>called from CreateFromCurve when a single radius value is supplied</remark>
        /// <param name="vCurves">curves to divide into points</param>
        /// <param name="cRadius">desired radius value for the points of each curve</param>
        /// <param name="vPoints">list to store all points for every curve</param>
        /// <param name="vRadius">list to store all radius values for every point in vPoints</param>
        /// <returns>boolean with whether operation was successful</returns>
        private bool ResolveSingleRadius (List<Curve> vCurves, double cRadius, out List<Point3d> vPoints, out List<double> vRadius) {
            vRadius = new List<double> ();
            vPoints = new List<Point3d> ();

            if (cRadius > 0) {
                // divide every curve provided into points
                foreach (Curve crv in vCurves) {
                    List<Point3d> vp = this.CurveToPoints (crv, cRadius);
                    vPoints.AddRange (vp);
                }

                // make a radius list which is the same size of point set
                vRadius = Enumerable.Repeat (cRadius, vPoints.Count).ToList ();

            }
            else {
                return false;
            }

            return true;
        }

        /// <summary>
        /// solves whether single or multiple radius values were provided to CreateFromCurve()
        /// </summary>
        /// <remark>this is used to tell CreateFromCurve how to proceed in dividing curves into points</remark>
        /// <param name="cCount">curve count</param>
        /// <param name="rCount">radius count</param>
        /// <returns>method needed for breaking curves into point (1 - single radius provided, 2 - multiple radius provided)</returns>
        private int GetCurveSolverMethod (int cCount, int rCount) {
            // no radius provided
            if (rCount == 0) {
                return 0;
            }

            // single radius provided
            if (rCount == 1) {
                return 1;
            }

            // multiple radius provided (equal in amount to curves provided)
            if (rCount == cCount) {
                return 2;
            }

            return 0;
        }

        /// <summary>
        /// divide a curve into point based on a desired radius value
        /// </summary>
        /// <param name="crv">curve to divide</param>
        /// <param name="radius">desired point radius value</param>
        /// <returns>list of divided points from curve</returns>
        private List<Point3d> CurveToPoints (Curve crv, double radius) {
            var cParams = crv.DivideByLength (radius * 0.25, true);
            List<Point3d> cPoints = new List<Point3d> ();

            foreach (double param in cParams) {
                Point3d pt = crv.PointAt (param);
                cPoints.Add (pt);
            }

            return cPoints;
        }
#endregion Helpers

    }
}