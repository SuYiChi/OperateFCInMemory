
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;

namespace ConsoleApp1
{
    class Program
    {
        private static LicenseInitializer m_AOLicenseInitializer = new ConsoleApp1.LicenseInitializer();

        static void Main(string[] args)
        {
            try
            {
                ESRI.ArcGIS.RuntimeManager.Bind(ESRI.ArcGIS.ProductCode.Desktop);

                //ESRI License Initializer generated code.
                //m_AOLicenseInitializer.InitializeApplication(new esriLicenseProductCode[] { esriLicenseProductCode.esriLicenseProductCodeStandard },
                //new esriLicenseExtensionCode[] { });
                //ESRI License Initializer generated code.
                //Do not make any call to ArcObjects after ShutDownApplication()
                Console.WriteLine("Initial Desktop License");
                Console.WriteLine("Press Enter to start process...");
                Console.ReadLine();

                // 在記憶體中建立空間資料庫
                InMemoryGDB pInMemoryGDB = new InMemoryGDB("COAWorkspace");
                pInMemoryGDB.CreateInstance();

                // 新增featureclass
                IFeatureClass pFC = pInMemoryGDB.CreateFC("MyFeatureClass");

                // Bulk insert feature
                List<IPoint> points = new List<IPoint>();
                pInMemoryGDB.InsertFeature(points, ref pFC);

                Console.WriteLine("Press Enter to stop process.");
                Console.ReadLine();

                m_AOLicenseInitializer.ShutdownApplication();
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }
        }
    }

    class InMemoryGDB
    {
        private IWorkspace pWorkspace;
        private readonly string workspaceName;

        public InMemoryGDB(string name)
        {
            workspaceName = name;
        }

        public InMemoryGDB()
        {
            workspaceName = "MyWorkspace";
        }

        /// <summary>
        /// 建立記憶體中的workspace
        /// </summary>
        public void CreateInstance()
        {
            // Create an in-memory workspace factory.
            IWorkspaceFactory2 pWorkspaceFactory;
            pWorkspaceFactory = new InMemoryWorkspaceFactoryClass() as IWorkspaceFactory2;

            // Create a new in-memory workspace. This returns a name object.
            IWorkspaceName pWorkspaceName = pWorkspaceFactory.Create(null, workspaceName, null, 0);
            IName pName = (IName)pWorkspaceName;

            // Open the workspace through the name object.
            //IWorkspace workspace = (IWorkspace)name.Open();
            pWorkspace = (IWorkspace)pName.Open();

        }

        /// <summary>
        /// 建立featureclass
        /// </summary>
        /// <param name="featureClassName"></param>
        /// <returns></returns>
        public IFeatureClass CreateFC(string featureClassName)
        {
            if (pWorkspace == null) throw new NullReferenceException("尚未建立Wokspace, " + nameof(pWorkspace) + " NullReference");

            IFeatureClass pFC = null;
            UID pCLSID = null;

            if (pCLSID == null)
            {
                pCLSID = new UID();
                pCLSID.Value = "esriGeoDatabase.Feature";
            }

            IFeatureWorkspace pFeatureWorkspace = (IFeatureWorkspace)pWorkspace;

            IObjectClassDescription objectClassDescription = new FeatureClassDescriptionClass();

            // create the fields using the required fields method 
            IFields fields = objectClassDescription.RequiredFields;
            IFieldsEdit fieldsEdit = (IFieldsEdit)fields;

            // Create a geometry definition (and spatial reference) for the feature class.
            // GeometryType=point and SpatialReference=WGS84
            IGeometryDef geometryDef = new GeometryDefClass();
            IGeometryDefEdit geometryDefEdit = (IGeometryDefEdit)geometryDef;
            geometryDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPoint;
            ISpatialReferenceFactory spatialReferenceFactory = new SpatialReferenceEnvironmentClass();
            ISpatialReference spatialReference =
            spatialReferenceFactory.CreateGeographicCoordinateSystem((int)esriSRGeoCSType.esriSRGeoCS_WGS1984);
            ISpatialReferenceResolution spatialReferenceResolution = (ISpatialReferenceResolution)spatialReference;
            spatialReferenceResolution.ConstructFromHorizon();
            ISpatialReferenceTolerance spatialReferenceTolerance = (ISpatialReferenceTolerance)spatialReference;
            spatialReferenceTolerance.SetDefaultXYTolerance();
            geometryDefEdit.SpatialReference_2 = spatialReference;

            // Set spatialReference
            for (int i = 0; i < fields.FieldCount; i++)
            {
                var pfield = fields.get_Field(i);
                if (pfield.Type == esriFieldType.esriFieldTypeGeometry)
                {
                    var geometryField = pfield as IFieldEdit;
                    geometryField.GeometryDef_2 = geometryDef;
                    Console.WriteLine(pfield.GeometryDef.SpatialReference.Name);
                    break;
                }
            }

            // create a user defined text field  
            IField field = new FieldClass();
            IFieldEdit fieldEdit = (IFieldEdit)field;

            // setup field properties  
            fieldEdit.Name_2 = "SampleField";
            fieldEdit.Type_2 = esriFieldType.esriFieldTypeString;
            fieldEdit.IsNullable_2 = true;
            fieldEdit.AliasName_2 = "Sample Field Column";
            fieldEdit.DefaultValue_2 = "test";
            fieldEdit.Editable_2 = true;
            fieldEdit.Length_2 = 100;

            // add field to field collection  
            fieldsEdit.AddField(field);
            fields = (IFields)fieldsEdit;

            String strShapeField = "";

            // locate the shape field  
            for (int j = 0; j < fields.FieldCount; j++)
            {
                if (fields.get_Field(j).Type == esriFieldType.esriFieldTypeGeometry)
                {
                    strShapeField = fields.get_Field(j).Name;
                }
            }

            //create feature class in memory workspace  
            pFC = pFeatureWorkspace.CreateFeatureClass(featureClassName, fields, pCLSID, null, esriFeatureType.esriFTSimple, strShapeField, null);

            return pFC;
        }

        /// <summary>
        /// 批次新增圖徵
        /// </summary>
        /// <param name="points"></param>
        /// <param name="featureclass"></param>
        public void InsertFeature(List<IPoint> points, ref IFeatureClass featureclass)
        {
            if (featureclass == null) throw new ArgumentNullException(nameof(featureclass));
            if (points == null || points.Count == 0) throw new ArgumentNullException(nameof(points));

            IFeatureCursor pFeatCur = featureclass.Insert(true);
            int mustFlush = 1000; //每n筆儲存一次
            int featureCount = 0;

            foreach (var item in points)
            {
                IFeatureBuffer pFeatBuffer = featureclass.CreateFeatureBuffer();
                pFeatBuffer.Shape = item;
                pFeatCur.InsertFeature(pFeatBuffer);
                ++featureCount;
                if (featureCount % mustFlush == 0)
                    pFeatCur.Flush();
            }

            pFeatCur.Flush();
        }
    }
}


