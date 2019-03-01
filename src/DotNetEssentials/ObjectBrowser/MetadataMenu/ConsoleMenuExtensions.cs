using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Intership.Core.ConsoleMenu;

namespace ObjectBrowser.MetadataMenu
{
	public static class ConsoleMenuExtensions
	{
		// keeps instance of StringBuilder per thread. Don't forget to clear it after usage ;-)
		private static readonly ThreadLocal<StringBuilder> StringBuilderLocal = new ThreadLocal<StringBuilder>(() => new StringBuilder(64));

		public static CompositeMenuCommand GetAssembliesMenuItem(this IEnumerable<Assembly> assemblies, string header, IMenuCommand parent = null)
		{
			if (assemblies == null)
			{
				throw new ArgumentNullException(nameof(assemblies));
			}

			var result = new CompositeMenuCommand(header, parent ?? ExitMenuCommand.Instance);

			var commands = assemblies
				.OrderBy(asm => asm.GetName().Name)
				.Select(x => GetAssemblyTypesMenuItem(x, result));

			result.AddCommands(commands);

			return result;
		}

		private static CompositeMenuCommand GetAssemblyTypesMenuItem(this Assembly assembly, IMenuCommand parent)
		{
			if (assembly == null)
			{
				throw new ArgumentNullException(nameof(assembly));
			}

			var result = new CompositeMenuCommand(assembly.FullName, parent);

			result.AddCommands(assembly.GetExportedTypes().OrderBy(x => x.FullName).Select(x => GetTypeMenuItem(x, result)));

			return result;
		}

		private static CompositeMenuCommand GetTypeMenuItem(this Type type, IMenuCommand parent)
		{
			if (type == null)
			{
				throw new ArgumentNullException(nameof(type));
			}

			var result = new CompositeMenuCommand(type.FullName, parent);

			result.AddCommands(type.GetMembers().OrderBy(x => x.MemberType).ThenBy(x => x.Name).Select(x => GetMemberMenuItem(x, result)));

			return result;
		}

		private static ActionMenuCommand GetMemberMenuItem(this MemberInfo memberInfo, IMenuCommand parent)
		{
			if (memberInfo == null)
			{
				throw new ArgumentNullException(nameof(memberInfo));
			}

			return new ActionMenuCommand(
				GetMemberInfoString(memberInfo),
				() => PrintMemberInfoProperties(memberInfo))
			{
				Parent = parent
			};
		}

		private static void PrintMemberInfoProperties(MemberInfo memberInfo)
		{
            
            var memberInfoType = memberInfo.GetType(); // get concrete type in runtime
            
            

			var properties = memberInfoType.GetProperties();

			var maxPropertyNameLength = properties.Max(x => x.Name.Length);

			Console.WriteLine(GetMemberInfoString(memberInfo));

			var format = "\t{0,-" + maxPropertyNameLength.ToString() + "}: {1}";

            if (memberInfo.MemberType == MemberTypes.Method)
            {
                MethodInfo method = (MethodInfo)memberInfo;
                foreach (var param in method.GetParameters())
                {
                    //Console.WriteLine(format, param.Name, param.ParameterType); 
                    // Console.WriteLine($"\t\tisOptional:{param.IsOptional} \n\t\tDefault value:{param.DefaultValue} ");
                    Console.WriteLine(ParameterInfoString(param));


                }
            }
            foreach (var propertyInfo in properties.OrderBy(x => x.MemberType).ThenBy(x => x.Name))
			{
				Console.WriteLine(format, propertyInfo.Name, propertyInfo.GetValue(memberInfo));
               
            }
		}


        //Method for parameter browser 
        //private static void PrintParamsOfIntsance(PropertyInfo propertyInfo)
        //{

        //    var properties = propertyInfo.Ge;

        //    var maxPropertyNameLength = properties.Max(x => x.Name.Length);

        //    Console.WriteLine(GetMemberInfoString(memberInfo));

        //    var format = "\t{0,-" + maxPropertyNameLength.ToString() + "}: {1}";

        //    foreach (var propertyInfo in properties.OrderBy(x => x.MemberType).ThenBy(x => x.Name))
        //    {
        //        Console.WriteLine(format, propertyInfo.Name, propertyInfo.GetValue(memberInfo));
        //    }
        //}

        private static string ParameterInfoString(ParameterInfo parameter)
        {
            var stringBuilder = new StringBuilder($"\tParameter: {parameter.Name} \n\tParameter type: {parameter.ParameterType}\n")
                .Append($"\t\t\tisOptional:{parameter.IsOptional}\n")
                .Append($"\t\t\tisHas defalt value:{parameter.HasDefaultValue}\n")
                .Append($"\t\t\tisDefalt value:{parameter.DefaultValue}\n");



            return stringBuilder.ToString();
        }

        private static string GetMemberInfoString(MemberInfo memberInfo)
		{
			if (memberInfo is MethodInfo methodInfo)
			{
				var builder = StringBuilderLocal.Value
					.Append("Method: ")
					.Append(methodInfo.ReturnType.Name)
					.Append(' ')
					.Append(methodInfo.Name)
					.AppendFormat("({0})", string.Join(", ", methodInfo.GetParameters().Select(parameterInfo => parameterInfo.ParameterType.Name)));

				try
				{
					return builder.ToString();
				}
				finally
				{
					builder.Clear();
				}
			}

			return $"{memberInfo.MemberType} {memberInfo.Name}";
		}
	}
}