using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;

namespace DZprojectParameterPipe
{
    [Transaction(TransactionMode.Manual)]
    public class Main : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            var categorySet = new CategorySet();
            categorySet.Insert(Category.GetCategory(doc, BuiltInCategory.OST_PipeCurves));

            using (Transaction ts = new Transaction(doc, "Add parameter"))
            {
                ts.Start();
                CreateSharedParameter(uiapp.Application, doc, "Наименование", categorySet, BuiltInParameterGroup.PG_TEXT, true);
                ts.Commit();
            }

            List<Pipe> pipes = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_PipeCurves)
                .WhereElementIsNotElementType()
                .Cast<Pipe>()
                .ToList();

            using (Transaction ts = new Transaction(doc, "Set parameters"))
            {
                ts.Start();
                foreach (var pipe in pipes)
                {
                    Parameter inDiamParameter = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_INNER_DIAM_PARAM);
                    Parameter outDiamParameter = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_OUTER_DIAMETER);
                    Parameter pipeNameParameter = pipe.LookupParameter("Наименование");
                    pipeNameParameter.Set("Труба " + outDiamParameter.AsValueString() + "/" + inDiamParameter.AsValueString());
                }
                ts.Commit();
            }

            return Result.Succeeded;
        }

        private void CreateSharedParameter(Application application, Document doc, string parameterName, CategorySet categorySet, BuiltInParameterGroup builtInParameterGroup, bool isInstance)
        {
            DefinitionFile definitionFile = application.OpenSharedParameterFile();
            if (definitionFile == null)
            {
                TaskDialog.Show("Ошибка", "Не найден файл общих параметров");
            }
            Definition definition = definitionFile.Groups.
                SelectMany(group => group.Definitions).
                FirstOrDefault(def => def.Name.Equals(parameterName));
            if (definition == null)
            {
                TaskDialog.Show("Ошибка", "Не найден указанный параметр");
                return;
            }

            Binding binding = application.Create.NewTypeBinding(categorySet);
            if (isInstance)
            {
                binding = application.Create.NewInstanceBinding(categorySet);
            }

            BindingMap map = doc.ParameterBindings;
            map.Insert(definition, binding, builtInParameterGroup);
        }
    }
}