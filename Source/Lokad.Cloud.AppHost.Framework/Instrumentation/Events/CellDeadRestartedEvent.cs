#region Copyright (c) Lokad 2011-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Xml.Linq;

namespace Lokad.Cloud.AppHost.Framework.Instrumentation.Events
{
    [Serializable]
    public class CellDeadRestartedEvent : IHostEvent
    {
        public HostEventLevel Level { get { return HostEventLevel.FatalError; } }
        public HostLifeIdentity Host { get; private set; }
        public string CellName { get; private set; }
        public string SolutionName { get; private set; }

        public CellDeadRestartedEvent(HostLifeIdentity host, string cellName, string solutionName)
        {
            Host = host;
            CellName = cellName;
            SolutionName = solutionName;
        }

        public string Describe()
        {
            return string.Format("AppHost: {0} cell of {1} solution was found dead on {2} and will be resurrected.",
                CellName, SolutionName, Host.WorkerName);
        }

        public XElement DescribeMeta()
        {
            return new XElement("Meta",
                new XElement("Component", "Lokad.Cloud.AppHost"),
                new XElement("Event", "CellDeadRestartedEvent"),
                new XElement("AppHost",
                    new XElement("Host", Host.WorkerName),
                    new XElement("Solution", SolutionName),
                    new XElement("Cell", CellName)));
        }
    }
}
