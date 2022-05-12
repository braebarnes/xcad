﻿using System;
using System.Collections.Generic;
using System.Text;
using Xarial.XCad.UI.PropertyPage.Base;

namespace Xarial.XCad.UI.PropertyPage.Attributes
{
    /// <summary>
    /// Indicates that this control should not raise the <see cref="IXPropertyPage{TDataModel}.DataChanged"/> notification
    /// </summary>
    public class SilentControlAttribute : Attribute, ISilentBindingAttribute
    {
    }
}
