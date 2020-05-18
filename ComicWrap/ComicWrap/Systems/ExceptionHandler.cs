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

            // Re-throw exception here so we don't miss it among all the stuff in the console
            throw e;
        }
    }
}
