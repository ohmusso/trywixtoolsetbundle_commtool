using System;
using System.Collections.Generic;
using System.Linq;
using WixToolset.Dtf.WindowsInstaller;

namespace CustomAction
{
    public class CustomActions
    {
        [CustomAction]
        public static ActionResult CustomAction(Session session)
        {
            session.Log("Begin CustomAction");

            return ActionResult.Success;
        }
    }
}
