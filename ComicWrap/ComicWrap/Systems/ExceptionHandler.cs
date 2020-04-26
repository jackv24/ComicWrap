using System;
using System.Collections.Generic;
using System.Text;

namespace ComicWrap.Systems
{
    public static class ExceptionHandler
    {
        public static void LogException(Exception e)
        {
            Console.WriteLine(e);
        }
    }
}
