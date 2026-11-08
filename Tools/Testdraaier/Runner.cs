using System; using System.Linq; using System.Reflection; using System.Diagnostics;
class P{ static int Main(string[] a){
 var asm=Assembly.LoadFrom(a[0]); int ok=0,fail=0;
 foreach(var t in asm.GetTypes()) foreach(var m in t.GetMethods()){
  var attrs=m.GetCustomAttributes().Select(x=>x.GetType().Name).ToList();
  object[][] cases=null;
  if(attrs.Contains("TestAttribute")) cases=new[]{new object[0]};
  else if(attrs.Contains("TestCaseAttribute")) cases=m.GetCustomAttributes().Where(x=>x.GetType().Name=="TestCaseAttribute").Select(x=>(object[])x.GetType().GetProperty("Arguments").GetValue(x)).Select(args=>args.Select((v,i)=>Convert.ChangeType(v,m.GetParameters()[i].ParameterType)).ToArray()).ToArray();
  else if(attrs.Contains("TestCaseSourceAttribute")){ var at=m.GetCustomAttributes().First(x=>x.GetType().Name=="TestCaseSourceAttribute"); var name=(string)at.GetType().GetProperty("SourceName").GetValue(at);
    var src=(System.Collections.IEnumerable)t.GetField(name,BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public).GetValue(null); cases=src.Cast<object>().Select(o=>new object[]{o}).ToArray(); }
  else continue;
  foreach(var c in cases){ var sw=Stopwatch.StartNew(); try{ m.Invoke(Activator.CreateInstance(t),c); ok++; Console.WriteLine($"OK   {t.Name}.{m.Name} {(c.Length==1&&c[0] is int[] ia?string.Join("x",ia):string.Join(",",c))} ({sw.ElapsedMilliseconds} ms)"); }
   catch(TargetInvocationException e){ fail++; Console.WriteLine($"FOUT {t.Name}.{m.Name}: {e.InnerException.GetType().Name}: {e.InnerException.Message}"); } }
 }
 Console.WriteLine($"{ok} geslaagd, {fail} mislukt"); return fail; } }
