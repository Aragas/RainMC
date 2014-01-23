#include <Windows.h>

using namespace System;
using namespace System::Reflection;
using namespace System::Runtime::InteropServices;

#define PLUGIN_EXPORT EXTERN_C __declspec(dllexport)

PLUGIN_EXPORT void Initialize(void** data, void* rm)
{
	IntPtr dataPtr = (IntPtr)*data;
	Plugin::Plugin::Initialize(dataPtr, (IntPtr)rm);
	*data = (void*)dataPtr;
}

PLUGIN_EXPORT void Finalize(void* data)
{
	Plugin::Plugin::Finalize((IntPtr)data);
}

PLUGIN_EXPORT void Reload(void* data, void* rm, double* maxValue)
{
	double maxValueDouble = *maxValue;
	Plugin::Plugin::Reload((IntPtr)data, (IntPtr)rm, maxValueDouble);
	*maxValue = maxValueDouble;	
}

PLUGIN_EXPORT double Update(void* data)
{
	return Plugin::Plugin::GetDouble((IntPtr)data);
}

PLUGIN_EXPORT LPCWSTR GetString(void* data)
{
	static const IntPtr zeroPtr = IntPtr::Zero;
	static IntPtr buffer = zeroPtr;

	Marshal::FreeHGlobal(buffer);
	IntPtr output = Plugin::Plugin::GetString((IntPtr)data);

	if (output != IntPtr::Zero)
		return (LPCWSTR)(void*)output;
	
	buffer = zeroPtr;
	return NULL;
}

PLUGIN_EXPORT void ExecuteBang(void* data, LPCWSTR args)
{
	Plugin::Plugin::ExecuteBang((IntPtr)data, (IntPtr)(void*)args);
}

class AssemblyLoader
{
	static Assembly^ LoadAssembly(Object^ sender, ResolveEventArgs^ args)
	{
		Assembly ^assembly = nullptr;
		System::IO::Stream ^stream = Assembly::GetExecutingAssembly()->GetManifestResourceStream((gcnew AssemblyName(args->Name))->Name + ".dll");
	
		try
		{		
			array<byte> ^assemblyData = gcnew array<byte>((int)(stream->Length));
			stream->Read(assemblyData, 0, assemblyData->Length);
			assembly = Assembly::Load(assemblyData);
		}
		finally
		{
			if (stream != nullptr)
				stream->Close();
		}

		return assembly;
	}

public:
	AssemblyLoader()
	{
		AppDomain::CurrentDomain->AssemblyResolve += gcnew ResolveEventHandler(&LoadAssembly);
	}
};

AssemblyLoader assemblyLoader;
