﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace OpenGLF
{
    [Serializable]
    public class BlockDraw : BlockControl
    {
        public BlockDraw(Blueprint owner) : base(owner)
        {
            
        }

        public override void draw()
        {
            doNext();
        }
    }
}
