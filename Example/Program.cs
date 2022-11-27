using TypeExtensions.Generated;
internal class Program
{
    [EmitType] private static string SampleType { get;  } = "{ \"boolVal\" : true, \"numVal\" : 23.69, \"objVal\" : {\"nestedField\" : \"23\"}, \"arrVal\" : [23, 69], \"arrobjVal\" : [{\"nestedField\" : \"23\"}] }";

    private static void Main(string[] args)
    {
        SampleType test = new SampleType();
        test.boolVal = true;
        test.numVal = 23.69M;
        test.objVal = new Objval_T
        {
            nestedField = "12"
        };
    }
}