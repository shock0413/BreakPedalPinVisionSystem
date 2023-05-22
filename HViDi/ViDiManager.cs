using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViDi2;
using Cognex.VisionPro;
using ViDi2.Runtime;
using ViDi2.VisionPro;
using ViDi2.VisionPro.Records;
using System.Runtime.InteropServices;
using System.Drawing;

namespace HViDi
{
    public class ViDiManager
    {
        ViDi2.Runtime.Local.Control control;

        public ViDiManager()
        {
            InitSystem();
        }

        private void InitSystem()
        {
            control = new ViDi2.Runtime.Local.Control(GpuMode.Deferred);
            control.InitializeComputeDevices(GpuMode.SingleDevicePerTool, new List<int>() { });
        }

        public void LoadTool(string workspaceName, string workspacePath)
        {
            if (control.Workspaces.Count(x => x.Name == workspaceName) == 0)
            {
                Console.WriteLine("Add Workspace : " + workspaceName);
                control.Workspaces.Add(workspaceName, workspacePath);
            }
        }

        public ViDiTotalResult Run(string workspaceName, string streamName, int height, int width, int channels, int stride, byte[] data)
        {
            ViDiTotalResult totalResult = new ViDiTotalResult();

            List<ViDiResult> result = new List<ViDiResult>();
            Record recordContainer;

            List<ICogRecord> records = new List<ICogRecord>();

            ViDi2.IWorkspace workspace = control.Workspaces[workspaceName];
            ViDi2.IStream stream = workspace.Streams[streamName];

            using (ByteImage image = new ByteImage(width, height, channels, ImageChannelDepth.Depth8, data, stride))
            {
                using (ISample sample = stream.CreateSample(image))
                {
                    for (int i = 0; i < stream.Tools.Count; i++)
                    {
                        ViDi2.ITool tool = stream.Tools.ElementAt(i);

                        ViDiResult currentResult = new ViDiResult();
                        sample.Process(tool);
                        IMarking marking = sample.Markings[tool.Name];

                        records.Add(CreateToolRecordFromMarking(marking, image, tool.Name));
                        currentResult.toolName = marking.ToolName.ToString();
                        currentResult.record = records;

                        for (int j = 0; j < marking.Views.Count; j++)
                        {
                            IBlueView blueView = marking.Views[j] as IBlueView;

                            if (blueView != null)
                            {
                                currentResult.features.AddRange(blueView.Features);
                                currentResult.matches.AddRange(blueView.Matches);
                            }
                            else
                            {
                                IGreenView greenView = marking.Views[j] as IGreenView;
                                if (greenView != null)
                                {
                                    currentResult.tags.Add(greenView.BestTag);
                                }
                            }
                        }

                        result.Add(currentResult);

                        List<ViDiResult> childResult = RunChild(tool, sample, image);
                        for (int j = 0; j < childResult.Count; j++)
                        {
                            records.AddRange(childResult[j].record);
                        }
                        result.AddRange(childResult);
                    }
                }

                IntPtr bitmapPtr = Marshal.AllocHGlobal(data.Length);
                Marshal.Copy(data, 0, bitmapPtr, data.Length);
                Bitmap bitmap = new Bitmap(width, height, stride, System.Drawing.Imaging.PixelFormat.Format24bppRgb, bitmapPtr);

                ICogImage cogimage = new CogImage24PlanarColor(bitmap);

                recordContainer = new ViDi2.VisionPro.Records.Record(cogimage, "Results");
                cogimage = null;
                for (int i = 0; i < records.Count; i++)
                {
                    recordContainer.SubRecords.Add(records[i]);
                }

                bitmap.Dispose();
                bitmap = null;
                Marshal.FreeHGlobal(bitmapPtr);
            }

            totalResult.results = result;
            totalResult.record = recordContainer;

            return totalResult;
        }

        private ICogRecord CreateToolRecordFromMarking(IMarking marking, IImage image, string toolName)
        {
            ICogRecord toolRecord = null;

            if (marking is IBlueMarking)
            {
                toolRecord = new BlueToolRecord((IBlueMarking)marking, image, toolName);
            }
            else if (marking is IRedMarking)
            {
                var redToolRecord = new RedToolRecord((IRedMarking)marking, image, toolName);

                if (redToolRecord.HasHeatMap())
                {
                    redToolRecord.SetHeatMapGraphicsVisibility(true);
                }

                toolRecord = redToolRecord;
            }
            else if (marking is IGreenMarking)
            {
                toolRecord = new GreenToolRecord((IGreenMarking)marking, image, toolName);
            }

            return toolRecord;
        }

        private List<ViDiResult> RunChild(ViDi2.ITool tool, ISample sample, IImage image)
        {
            List<ViDiResult> result = new List<ViDiResult>();

            for (int i = 0; i < tool.Children.Count; i++)
            {
                List<ICogRecord> records = new List<ICogRecord>();

                ViDiResult currentResult = new ViDiResult();
                sample.Process(tool.Children.ElementAt(i));
                IMarking marking = sample.Markings[tool.Children.ElementAt(i).Name];
                records.Add(CreateToolRecordFromMarking(marking, image, tool.Children.ElementAt(i).Name));
                currentResult.toolName = marking.ToolName.ToString();
                currentResult.record = records;
                for (int j = 0; j < marking.Views.Count; j++)
                {
                    IBlueView blueView = marking.Views[j] as IBlueView;
                    if (blueView != null)
                    {
                        currentResult.features.AddRange(blueView.Features);
                        currentResult.matches.AddRange(blueView.Matches);
                    }
                    else
                    {
                        IGreenView greenView = marking.Views[j] as IGreenView;
                        if (greenView != null)
                        {
                            currentResult.tags.Add(greenView.BestTag);
                        }
                    }

                }


                result.Add(currentResult);
                List<ViDiResult> chilldResults = RunChild(tool.Children.ElementAt(i), sample, image);
                result.AddRange(chilldResults);
            }

            return result;
        }
    }
}
