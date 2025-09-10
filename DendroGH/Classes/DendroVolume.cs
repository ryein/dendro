using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Rhino.Geometry;

namespace DendroGH
{
    /// <summary>
    /// c# wrapper for c++ api. this holds all external dll calls and is
    /// primary point of communication with all openvdb functions. the mGrid
    /// member is a pointer to a c++ class and all DendroVolume methods operate
    /// specific functions on that c++ mGrid class
    /// </summary>
    public class DendroVolume : IDisposable
    {

        [StructLayout(LayoutKind.Sequential)]
        struct NativePoint { public double X, Y, Z; }

        [StructLayout(LayoutKind.Sequential)]
        struct NativeFace { public int A, B, C; }

        #region PInvokes
#if UNIX
        [DllImport("libDendroAPI.dylib", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("DendroAPI.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        static private extern IntPtr DendroCreate();

#if UNIX
        [DllImport("libDendroAPI.dylib", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("DendroAPI.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        static private extern void DendroDelete(IntPtr grid);

#if UNIX
        [DllImport("libDendroAPI.dylib", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("DendroAPI.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        static private extern IntPtr DendroDuplicate(IntPtr grid);

#if UNIX
        [DllImport("libDendroAPI.dylib", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("DendroAPI.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        static private extern bool DendroRead(IntPtr grid, string filename);

#if UNIX
        [DllImport("libDendroAPI.dylib", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("DendroAPI.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        static private extern bool DendroWrite(IntPtr grid, string filename);

#if UNIX
        [DllImport("libDendroAPI.dylib", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("DendroAPI.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        static private extern bool DendroFromMesh(IntPtr grid, [In] NativePoint[] vertices, int vCount, [In] NativeFace[] faces, int fCount, double voxelSize, double bandwidth);
#if UNIX
        [DllImport("libDendroAPI.dylib", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("DendroAPI.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        static extern bool DendroFromPoints(IntPtr grid, IntPtr pts, nuint count, [In] double[] radii, int rCount, double voxelSize, double bandwidth);

#if UNIX
        [DllImport("libDendroAPI.dylib", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("DendroAPI.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        static private extern bool DendroToMesh(IntPtr grid, out IntPtr vertices, out int vCount, out IntPtr faces, out int fCount, double isovalue, double adaptivity);

#if UNIX
        [DllImport("libDendroAPI.dylib", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("DendroAPI.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        static private extern void DendroFree(IntPtr ptr);

#if UNIX
        [DllImport("libDendroAPI.dylib", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("DendroAPI.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        static private extern bool DendroTransform(IntPtr grid, double[] matrix, int mCount);

#if UNIX
        [DllImport("libDendroAPI.dylib", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("DendroAPI.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        static public extern void DendroUnion(IntPtr grid, IntPtr csgGrid);

#if UNIX
        [DllImport("libDendroAPI.dylib", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("DendroAPI.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        static public extern void DendroDifference(IntPtr grid, IntPtr csgGrid);

#if UNIX
        [DllImport("libDendroAPI.dylib", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("DendroAPI.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        static public extern void DendroIntersection(IntPtr grid, IntPtr csgGrid);

#if UNIX
        [DllImport("libDendroAPI.dylib", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("DendroAPI.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        static public extern void DendroOffset(IntPtr grid, double amount);

#if UNIX
        [DllImport("libDendroAPI.dylib", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("DendroAPI.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        static public extern void DendroOffsetMask(IntPtr grid, double amount, IntPtr mask, double min, double max, bool invert);

#if UNIX
        [DllImport("libDendroAPI.dylib", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("DendroAPI.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        static public extern void DendroSmooth(IntPtr grid, int type, int iterations, int width);

#if UNIX
        [DllImport("libDendroAPI.dylib", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("DendroAPI.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        static public extern void DendroSmoothMask(IntPtr grid, int type, int iterations, int width, IntPtr mask, double min, double max, bool invert);

#if UNIX
        [DllImport("libDendroAPI.dylib", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("DendroAPI.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        static public extern void DendroBlend(IntPtr bGrid, IntPtr eGrid, double bPosition, double bEnd);

#if UNIX
        [DllImport("libDendroAPI.dylib", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("DendroAPI.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        static public extern void DendroBlendMask(IntPtr bGrid, IntPtr eGrid, double bPosition, double bEnd, IntPtr mask, double min, double max, bool invert);

#if UNIX
        [DllImport("libDendroAPI.dylib", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("DendroAPI.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        static private extern IntPtr DendroClosestPoint(IntPtr grid, float[] vertices, int vCount, out int rSize);

        #endregion PInvokes

        #region Members
        private IntPtr mGrid; // stores pointer to grid in c++
        private bool mValid; // volume validity
        #endregion Members

        #region Constructors
        /// <summary>
        /// default constructor
        /// </summary>
        public DendroVolume()
        {
            // pinvoke grid creation
            this.Grid = DendroCreate();

            // volume is empty, so it is invalid
            this.IsValid = false;
        }

        /// <summary>
        /// copy constructor
        /// </summary>
        /// <param name="vCopy">volume to copy from</param>
        public DendroVolume(DendroVolume vCopy)
        {
            if (vCopy.IsValid)
            {
                this.Grid = vCopy.DuplicateGrid();

                this.IsValid = true;
            }
            else
            {
                // pinvoke grid creation
                this.Grid = DendroCreate();

                // volume is empty, so it is invalid
                this.IsValid = false;
            }
        }

        /// <summary>
        /// create volume from a vdb file
        /// </summary>
        /// <param name="vFile">full path and name of vdb file to read (vdb extension)</param>
        public DendroVolume(string vFile)
        {
            // create a new grid
            this.Grid = DendroCreate();

            // read vdb file
            this.IsValid = this.Read(vFile);
        }

        /// <summary>
        /// mesh constructor
        /// </summary>
        /// <param name="vMesh">mesh to build volume from</param>
        /// <param name="vSettings">voxelization settings to be used</param>
        public DendroVolume(Mesh vMesh, DendroSettings vSettings)
        {
            // pinvoke grid creation
            this.Grid = DendroCreate();

            this.IsValid = this.CreateFromMesh(vMesh, vSettings);
        }

        /// <summary>
        /// point constructor
        /// </summary>
        /// <remark>must supply a single radius value or a list of radii equal to the number of points supplied</remark>
        /// <param name="vPoints">point set to build volume from</param>
        /// <param name="vRadius">radius values for each point</param>
        /// <param name="vSettings">voxelization settings to be used</param>
        public DendroVolume(List<Point3d> vPoints, List<double> vRadius, DendroSettings vSettings)
        {
            // pinvoke grid creation
            this.Grid = DendroCreate();

            this.IsValid = this.CreateFromPoints(vPoints, vRadius, vSettings);
        }

        /// <summary>
        /// curve constructor
        /// </summary>
        /// <remark>must supply a single radius value or a list of radii equal to the number of curves supplied</remark>
        /// <param name="vCurves">curves to build volume from</param>
        /// <param name="vRadius">radius values for each curve</param>
        /// <param name="vSettings">voxelization settings to be used</param>
        public DendroVolume(List<Curve> vCurves, List<double> vRadius, DendroSettings vSettings)
        {
            // pinvoke grid creation
            this.Grid = DendroCreate();

            this.IsValid = this.CreateFromCurves(vCurves, vRadius, vSettings);
        }

        /// <summary>
        /// dispose of volume and release resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// protected implementation of dispose pattern
        /// </summary>
        /// <param name="bDisposing">holds value indicating if this was called from dispose or finalizer</param>
        protected virtual void Dispose(bool bDisposing)
        {
            if (this.Grid != IntPtr.Zero)
            {
                // cleanup everything on the c++ side
                DendroDelete(this.Grid);

                // clear grid pointer
                this.Grid = IntPtr.Zero;
                this.IsValid = false;
            }

            // finalize garbage collection
            if (bDisposing)
            {
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// destructor
        /// </summary>
        ~DendroVolume()
        {
            Dispose(false);
        }

        /// <summary>
        /// duplicate the volume grid
        /// </summary>
        /// <remarks>
        /// needed in addition to the copy contructor in order to duplicate just the c++ grid class
        /// </remarks>
        /// <returns>returns pointer to c++ grid</returns>
        public IntPtr DuplicateGrid()
        {
            // pinvoke grid duplication
            return DendroDuplicate(this.Grid);
        }
        #endregion Constructors

        #region Properties
        /// <summary>
        /// volume validity property
        /// </summary>
        /// <returns>boolean value of volume validity</returns>
        public bool IsValid
        {
            get
            {
                if (this.Grid == IntPtr.Zero) return false;
                return this.mValid;
            }
            private set
            {
                this.mValid = value;
            }
        }

        /// <summary>
        /// volume grid pointer property
        /// </summary>
        /// <returns>pointer to c++ grid</returns>
        public IntPtr Grid
        {
            get
            {
                return this.mGrid;
            }
            private set
            {
                this.mGrid = value;
            }
        }
        #endregion Properties

        #region Methods
        /// <summary>
        /// read a vdb file and build volume
        /// </summary>
        /// <param name="vFile">full path and name of vdb file to read (vdb extension)</param>
        /// <returns>boolean value for whether reading was successful</returns>
        public bool Read(string vFile)
        {
            if (!File.Exists(vFile))
            {
                return false;
            }

            // pinvoke file read function
            this.IsValid = DendroRead(this.Grid, vFile);

            if (!this.IsValid)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// write volume to a vdb file
        /// </summary>
        /// <param name="vFile">full path and name of vdb file to write (vdb extension)</param>
        /// <returns>boolean value for whether file write was successful</returns>
        public bool Write(string vFile)
        {
            // pinvoke file writing function
            DendroWrite(this.Grid, vFile);

            if (!File.Exists(vFile))
            {
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
        public bool CreateFromMesh(Mesh vMesh, DendroSettings vSettings)
        {
            if (!vMesh.IsValid)
                return false;

            // check for invalid voxelsize settings
            if (vSettings.VoxelSize < 0.01)
                vSettings.VoxelSize = 0.01;

            // check for invalid bandwidth settings
            if (vSettings.Bandwidth < 1)
                vSettings.Bandwidth = 1;

            int vCount = vMesh.Vertices.Count;

            NativePoint[] vertices = new NativePoint[vCount];
            for (int i = 0; i < vCount; ++i)
            {
                var p = vMesh.Vertices[i];
                vertices[i] = new NativePoint { X = p.X, Y = p.Y, Z = p.Z };
            }

            var faceList = new List<NativeFace>(vMesh.Faces.Count);
            foreach (var f in vMesh.Faces)
            {
                faceList.Add(new NativeFace { A = f.A, B = f.B, C = f.C });
                if (f.IsQuad)
                    faceList.Add(new NativeFace { A = f.A, B = f.C, C = f.D });
            }

            var faces = faceList.ToArray();
            this.IsValid = DendroFromMesh(this.Grid, vertices, vertices.Length, faces, faces.Length, vSettings.VoxelSize, vSettings.Bandwidth);

            if (!this.IsValid)
                return false;

            return true;
        }


        /// <summary>
        /// generate a mesh from the current volume
        /// </summary>
        /// <returns>mesh representation or null if conversion failed</returns>
        public Mesh ToMesh(DendroSettings vSettings)
        {
            if (!this.IsValid)
                return null;

            bool ok;
            IntPtr vPtr, fPtr;
            int vCount, fCount;

            ok = DendroToMesh(this.Grid, out vPtr, out vCount, out fPtr, out fCount, vSettings.IsoValue, vSettings.Adaptivity);

            if (!ok)
                return null;

            try
            {
                Mesh mesh = new Mesh();

                unsafe
                {

                    var vSpan = new ReadOnlySpan<NativePoint>(vPtr.ToPointer(), vCount);
                    var fSpan = new ReadOnlySpan<NativeFace>(fPtr.ToPointer(), fCount);

                    for (int i = 0; i < vCount; ++i)
                        mesh.Vertices.Add(vSpan[i].X, vSpan[i].Y, vSpan[i].Z);

                    for (int i = 0; i < fCount; ++i)
                        mesh.Faces.AddFace(fSpan[i].A, fSpan[i].B, fSpan[i].C);

                }

                mesh.Normals.ComputeNormals();
                mesh.Compact();
                return mesh;
            }
            finally
            {
                DendroFree(vPtr);
                DendroFree(fPtr);
            }
        }


        /// <summary>
        /// build a volume from a supplied list of points
        /// </summary>
        /// <remark>must supply a single radius value or a list of radii equal to the number of points supplied</remark>
        /// <param name="vPoints">point set to build volume from</param>
        /// <param name="vRadius">radius values for each point</param>
        /// <param name="vSettings">voxelization settings to be used</param>
        /// <returns>boolean value for whether volume was built successfully</returns>
        public bool CreateFromPoints(List<Point3d> vPoints, List<double> vRadius, DendroSettings vSettings)
        {
            // there were no points/radius supplied so exit
            if (vPoints.Count == 0 || vRadius.Count == 0) return false;

            // check for invalid voxelsize settings
            if (vSettings.VoxelSize < 0.01) vSettings.VoxelSize = 0.01;

            // check for invalid bandwidth settings
            if (vSettings.Bandwidth < 1) vSettings.Bandwidth = 1;

            // if one or equal radius values were supplied then build volume
            if (vPoints.Count == vRadius.Count || vRadius.Count == 1)
            {

                // create radius array from list so we can pass to c++
                double[] radius = vRadius.ToArray();

                // Get a span view over the list’s backing array
                var span = CollectionsMarshal.AsSpan(vPoints);

                // Reinterpret as our DendroPoint layout (three doubles)
                var dspan = MemoryMarshal.Cast<Point3d, NativePoint>(span);

                unsafe
                {
                    fixed (NativePoint* p = dspan)
                    {


                        this.IsValid = DendroFromPoints(this.Grid, (IntPtr)p, (nuint)dspan.Length, radius, radius.Length, vSettings.VoxelSize, vSettings.Bandwidth);

                        if (!this.IsValid) return false;

                        return true;
                    }
                }

            }

            return false;
        }

        /// <summary>
        /// build a volume from a supplied list of curves
        /// </summary>
        /// <remark>must supply a single radius value or a list of radii equal to the number of curves supplied</remark>
        /// <param name="vCurves">curves to build volume from</param>
        /// <param name="vRadius">radius values for each curve</param>
        /// <param name="vSettings">voxelization settings to be used</param>
        /// <returns>boolean value for whether volume was built successfully</returns>
        public bool CreateFromCurves(List<Curve> vCurves, List<double> vRadius, DendroSettings vSettings)
        {
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
            int method = GetCurveSolverMethod(vCurves.Count, vRadius.Count);

            bool validInput = false;
            List<double> rValues = new List<double>();
            List<Point3d> vPoints = new List<Point3d>();

            switch (method)
            {
                // only a single radius was supplied
                case 1:
                    validInput = ResolveSingleRadius(vCurves, vRadius[0], out vPoints, out rValues);
                    break;

                // multiple radius values were supplied
                case 2:
                    validInput = ResolveMultipleRadius(vCurves, vRadius, out vPoints, out rValues);
                    break;
                default:
                    validInput = false;
                    break;
            }

            // supplied values were not valid so exit
            if (!validInput)
                return false;

            // return results from point to volume function
            return this.CreateFromPoints(vPoints, rValues, vSettings);
        }

        /// <summary>
        /// gets the world axis aligned boundingbox for the volume
        /// </summary>
        /// <returns>boundingbox of the geometry in world coordinates or BoundingBox.Empty if not bounding box could be found</returns>
        public BoundingBox GetBoundingBox()
        {
            return BoundingBox.Empty;
        }

        /// <summary>
        /// gets the world axis aligned boundingbox for the transformed volume
        /// </summary>
        /// <param name="xform">transformation to apply to object prior to the bounding box computation</param>
        /// <returns>accurate boundingbox of the transformed geometry in world coordinates or BoundingBox.Empty if not bounding box could be found</returns>
        public BoundingBox GetBoundingBox(Transform xform)
        {
            return BoundingBox.Empty;
        }

        /// <summary>
        /// transform volume from supplied matrix
        /// </summary>
        /// <param name="xform">transform to apply to volume</param>
        /// <returns>boolean value for whether transform was successful</returns>
        public bool Transform(Transform xform)
        {
            if (!xform.IsValid)
                return false;

            // convert transform to flat array
            float[] floatMatrix = xform.ToFloatArray(false);
            double[] matrix = Array.ConvertAll(floatMatrix, x => (double)x);

            // pinvoke transform on grid
            this.IsValid = DendroTransform(this.Grid, matrix, matrix.Length);

            if (!this.IsValid)
                return false;

            return true;
        }

        /// <summary>
        /// compute a boolean difference of a volume
        /// </summary>
        /// <param name="vSubract">volume to subtract with</param>
        /// <returns>new volume with the resulting difference</returns>
        public DendroVolume BooleanDifference(DendroVolume vSubract)
        {
            if (!this.IsValid)
                return new DendroVolume();

            if (!vSubract.IsValid)
                return new DendroVolume(this);

            DendroVolume csg = new DendroVolume(this);

            // pinvoke difference function
            DendroDifference(csg.Grid, vSubract.Grid);

            return csg;
        }

        /// <summary>
        /// compute a boolean difference of a set of volumes
        /// </summary>
        /// <param name="vSubtract">list of volumes to subtract with</param>
        /// <returns>new volume with the resulting difference</returns>
        public DendroVolume BooleanDifference(List<DendroVolume> vSubtract)
        {
            if (!this.IsValid)
                return new DendroVolume();

            DendroVolume csg = new DendroVolume(this);

            foreach (DendroVolume subtract in vSubtract)
            {
                // pinvoke difference function
                if (subtract.IsValid)
                    DendroDifference(csg.Grid, subtract.Grid);
            }

            return csg;
        }

        /// <summary>
        /// compute a boolean intersection of a volume
        /// </summary>
        /// <param name="vIntersect">volume to intersect</param>
        /// <returns>new volume with the resulting intersection</returns>
        public DendroVolume BooleanIntersection(DendroVolume vIntersect)
        {
            if (!this.IsValid)
                return new DendroVolume();

            if (!vIntersect.IsValid)
                return new DendroVolume(this);

            DendroVolume csg = new DendroVolume(this);

            // pinvoke intersection function
            DendroIntersection(csg.Grid, vIntersect.Grid);

            return csg;
        }

        /// <summary>
        /// compute a boolean intersection of a set of volumes
        /// </summary>
        /// <param name="vIntersect">list of volumes to intersect</param>
        /// <returns>new volume with the resulting intersection</returns>
        public DendroVolume BooleanIntersection(List<DendroVolume> vIntersect)
        {
            if (!this.IsValid)
                return new DendroVolume();

            DendroVolume csg = new DendroVolume(this);

            foreach (DendroVolume intersect in vIntersect)
            {
                // pinvoke intersection function
                if (intersect.IsValid)
                    DendroIntersection(csg.Grid, intersect.Grid);
            }

            return csg;
        }

        /// <summary>
        /// compute a boolean union of a volume
        /// </summary>
        /// <param name="vUnion">volume to union</param>
        /// <returns>new volume with the resulting union</returns>
        public DendroVolume BooleanUnion(DendroVolume vUnion)
        {
            if (!this.IsValid)
                return new DendroVolume();

            if (!vUnion.IsValid)
                return new DendroVolume(this);

            DendroVolume csg = new DendroVolume(this);

            // pinvoke union function
            DendroUnion(csg.Grid, vUnion.Grid);

            return csg;
        }

        /// <summary>
        /// compute a boolean union of a set of volumes
        /// </summary>
        /// <param name="vUnion">list of volumes to union</param>
        /// <returns>new volume with the resulting union</returns>
        public DendroVolume BooleanUnion(List<DendroVolume> vUnion)
        {
            if (!this.IsValid)
                return new DendroVolume();

            DendroVolume csg = new DendroVolume(this);

            foreach (DendroVolume union in vUnion)
            {
                // pinvoke union function
                if (union.IsValid)
                    DendroUnion(csg.Grid, union.Grid);
            }

            return csg;
        }

        /// <summary>
        /// apply an offset to the volume
        /// </summary>
        /// <param name="amount">amount to offset volume</param>
        /// <returns>offset volume</returns>
        public DendroVolume Offset(double amount)
        {
            if (!this.IsValid)
                return new DendroVolume();

            DendroVolume offset = new DendroVolume(this);

            // pinvoke offset function
            DendroOffset(offset.Grid, amount);

            return offset;
        }

        /// <summary>
        /// apply an offset to the volume with a mask
        /// </summary>
        /// <param name="amount">amount to offset volume</param>
        /// <param name="vMask">mask for offset operation</param>
        /// <returns>offset volume</returns>
        public DendroVolume Offset(double amount, DendroMask vMask)
        {
            if (!this.IsValid)
                return new DendroVolume();

            DendroVolume offset = new DendroVolume(this);

            // pinvoke offset function with mask
            DendroOffsetMask(offset.Grid, amount, vMask.Volume.Grid, vMask.Min, vMask.Max, vMask.Invert);

            return offset;
        }

        /// <summary>
        /// apply smoothing to a volume
        /// </summary>
        /// <param name="sWidth">(optional) width of the mean-value filter is 2*width+1 voxels</param>
        /// <param name="sType">0 - gaussian, 1 - laplacian, 2 - mean, 3 - median</param>
        /// <param name="sIterations">number of smoothing operations to perform</param>
        /// <returns>smoothed volume</returns>
        public DendroVolume Smooth(int sType, int sIterations, int sWidth = 1)
        {
            if (!this.IsValid)
                return new DendroVolume();

            if (sType < 0 || sType > 3)
                sType = 1;

            if (sWidth < 1)
                sWidth = 1;

            if (sIterations < 1)
                sIterations = 1;

            DendroVolume smooth = new DendroVolume(this);

            // pinvoke smoothing function
            DendroSmooth(smooth.Grid, sType, sIterations, sWidth);

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
        public DendroVolume Smooth(int sType, int sIterations, DendroMask vMask, int sWidth = 1)
        {
            if (!this.IsValid)
                return new DendroVolume();

            if (sType < 0 || sType > 3)
                sType = 1;

            if (sWidth < 1)
                sWidth = 1;

            if (sIterations < 1)
                sIterations = 1;

            DendroVolume smooth = new DendroVolume(this);

            // pinvoke smoothing function with mask
            DendroSmoothMask(smooth.Grid, sType, sIterations, sWidth, vMask.Volume.Grid, vMask.Min, vMask.Max, vMask.Invert);

            return smooth;
        }

        /// <summary>
        /// blend two volumes
        /// </summary>
        /// <param name="bVolume">volume to blend with</param>
        /// <param name="bPosition">position parameter to sample blending at (normalized 0-1)</param>
        /// <returns>blended volume</returns>
        public DendroVolume Blend(DendroVolume bVolume, double bPosition, double bEnd)
        {
            if (!this.IsValid)
                return new DendroVolume();

            if (bPosition < 0) bPosition = 0;
            if (bPosition > 1) bPosition = 1;

            if (bEnd < 1) bEnd = 1;

            bPosition = 1 - bPosition;

            DendroVolume blend = new DendroVolume(this);

            // pinvoke smoothing function
            DendroBlend(blend.Grid, bVolume.Grid, bPosition, bEnd);

            return blend;
        }

        /// <summary>
        /// blend two volumes using a mask
        /// </summary>
        /// <param name="bVolume">volume to blend with</param>
        /// <param name="bPosition">position parameter to sample blending at (normalized 0-1)</param>
        /// <param name="vMask">mask for blending operation</param>
        /// <returns>blended volume</returns>
        public DendroVolume Blend(DendroVolume bVolume, double bPosition, double bEnd, DendroMask vMask)
        {
            if (!this.IsValid)
                return new DendroVolume();

            if (bPosition < 0) bPosition = 0;
            if (bPosition > 1) bPosition = 1;

            if (bEnd < 1) bEnd = 1;

            bPosition = 1 - bPosition;

            DendroVolume blend = new DendroVolume(this);

            // pinvoke smoothing function with mask
            DendroBlendMask(blend.Grid, bVolume.Grid, bPosition, bEnd, vMask.Volume.Grid, vMask.Min, vMask.Max, vMask.Invert);

            return blend;
        }

        public List<Point3d> ClosestPoint(List<Point3d> vPoints)
        {
            // create point array from point3d list so we can pass to c++
            float[] points = new float[vPoints.Count * 3];

            int i = 0;
            foreach (Point3d pt in vPoints)
            {
                points[i] = (float)pt.X;
                points[i + 1] = (float)pt.Y;
                points[i + 2] = (float)pt.Z;

                i += 3;
            }

            float[] cpArray = null; // array which holds reconstructed c++ array
            IntPtr cppPointer = IntPtr.Zero; // pointer to array on c++

            // pinvoke closest point function
            cppPointer = DendroClosestPoint(this.Grid, points, points.Length, out int size);


            if (cppPointer != IntPtr.Zero)
            {
                cpArray = new float[size];
                Marshal.Copy(cppPointer, cpArray, 0, size);
            }

            Marshal.FreeHGlobal(cppPointer);

            // convert array to Point3d list
            List<Point3d> cPoints = new List<Point3d>();
            int j = 0;
            while (j < size)
            {
                var cp = new Point3d(cpArray[j], cpArray[j + 1], cpArray[j + 2]);
                cPoints.Add(cp);

                j += 3;
            }

            return cPoints;
        }
        #endregion Methods

        #region Meshing
        #endregion Meshing

        #region Display
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
        private bool ResolveMultipleRadius(List<Curve> vCurves, List<double> cRadius, out List<Point3d> vPoints, out List<double> vRadius)
        {
            vRadius = new List<double>();
            vPoints = new List<Point3d>();

            int rIndex = 0;

            foreach (Curve crv in vCurves)
            {
                var radius = cRadius[rIndex];

                if (radius > 0)
                {

                    List<Point3d> vp = this.CurveToPoints(crv, radius);
                    List<double> rv = Enumerable.Repeat(radius, vp.Count).ToList();
                    vPoints.AddRange(vp);
                    vRadius.AddRange(rv);
                }
                else
                {
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
        private bool ResolveSingleRadius(List<Curve> vCurves, double cRadius, out List<Point3d> vPoints, out List<double> vRadius)
        {
            vRadius = new List<double>();
            vPoints = new List<Point3d>();

            if (cRadius > 0)
            {
                // divide every curve provided into points
                foreach (Curve crv in vCurves)
                {
                    List<Point3d> vp = this.CurveToPoints(crv, cRadius);
                    vPoints.AddRange(vp);
                }

                // make a radius list which is the same size of point set
                vRadius = Enumerable.Repeat(cRadius, vPoints.Count).ToList();

            }
            else
            {
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
        private int GetCurveSolverMethod(int cCount, int rCount)
        {
            // no radius provided
            if (rCount == 0)
            {
                return 0;
            }

            // single radius provided
            if (rCount == 1)
            {
                return 1;
            }

            // multiple radius provided (equal in amount to curves provided)
            if (rCount == cCount)
            {
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
        private List<Point3d> CurveToPoints(Curve crv, double radius)
        {

            List<Point3d> cPoints = new List<Point3d>();

            // Curve longer than a 1/4 of radius
            if (crv.GetLength() > radius * 0.25)
            {
                var cParams = crv.DivideByLength(radius * 0.25, true);
                foreach (double param in cParams)
                {
                    Point3d pt = crv.PointAt(param);
                    cPoints.Add(pt);
                }
            }
            // If curve too short add endpoints to point list
            else
            {
                cPoints.Add(crv.PointAtNormalizedLength(0));
                cPoints.Add(crv.PointAtNormalizedLength(1));
            }

            return cPoints;
        }
        #endregion Helpers

    }
}