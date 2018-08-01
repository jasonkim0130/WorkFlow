using System;

namespace Omnibackend.Api.Workflow.Logic
{
    /**
    * Created by jeremy on 12/8/2016 2:51:23 PM.
    */
    public class SqlPartitionHelper
    {
        public static void ExecuteWithPartitions<T>(T[] wholeKeys, Action<ArraySegment<T>> action)
        {
            if (wholeKeys.Length > 0)
            {
                int maxParameter = 50;
                for (int i = 0; i < (wholeKeys.Length / maxParameter + 1); i++)
                {
                    ArraySegment<T> subKeys = new ArraySegment<T>(wholeKeys, i * maxParameter,
                        wholeKeys.Length - i * maxParameter > maxParameter
                            ? maxParameter
                            : wholeKeys.Length - i * maxParameter);
                    action(subKeys);
                }
            }
        }
    }
}