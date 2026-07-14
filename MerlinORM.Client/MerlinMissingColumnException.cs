using System;
using System.Collections.Generic;
using System.Text;

namespace MerlinORM.Client
{
    public class MerlinMissingColumnException : MerlinException
    {



        public MerlinMissingColumnException(string Object, string MissingColumn, Exception inner) : base($"Unable to load '{Object}' data set missing column '{MissingColumn}'", inner)
        {

        }
    }
}
