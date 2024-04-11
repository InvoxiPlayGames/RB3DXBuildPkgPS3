using SCEllSharp.PKG;
using SCEllSharp.SFO;
using SCEllSharp.NPDRM;
using System.ComponentModel.DataAnnotations;

if (args.Length < 4)
{
    Console.WriteLine($"usage: RB3DXBuildPkgPS3 [/path/to/content/folder] [eur|usa] [content id suffix] [/path/to/out/dir]");
    Console.WriteLine($"   eg: RB3DXBuildPkgPS3 out/ps3 usa RB3DXNITESKIBIDI out");
    return;
}
string arg_content_path = args[0];
string arg_region = args[1];
string arg_id_suffix = args[2];
string arg_output_pkg = args[3];

// set the values to be used when building the pkg and PARAM.SFO
string content_id;
string title_id;
string title_version;
string title_name = "Rock Band 3"; // don't recommend changing this
if (arg_region == "usa")
{
    title_id = "BLUS30463";
    content_id = $"UP8802-{title_id}_00-{arg_id_suffix}";
    title_version = "01.05";
}
else if (arg_region == "eur")
{
    title_id = "BLES00986";
    content_id = $"EP0006-{title_id}_00-{arg_id_suffix}";
    title_version = "01.06";
}
else
{
    Console.Error.WriteLine("invalid region provided! must be eur or usa");
    return;
}

// some basic sanity checks. looks ugly but it's good
if (title_id.Length != 9)
{
    Console.Error.WriteLine($"title id is too long! \"{title_id}\" is {title_id.Length}, should be 9");
    Environment.ExitCode = -1;
    return;
}
if (content_id.Length != 36)
{
    Console.Error.WriteLine($"content id is too long! \"{content_id}\" is {content_id.Length}, should be 36");
    Environment.ExitCode = -2;
    return;
}

// if we don't have a pkg file output, treat it as a directory and add one
if (!arg_output_pkg.EndsWith(".pkg"))
{
    if (!Directory.Exists(arg_output_pkg)) Directory.CreateDirectory(arg_output_pkg);
    arg_output_pkg = Path.Join(arg_output_pkg, $"{content_id}.pkg");
}

if (!Directory.Exists(arg_content_path))
{
    Console.Error.WriteLine("Content files directory does not exist.");
    Environment.ExitCode = -3;
}

FileStream out_pkg;
try
{
    out_pkg = File.Create(arg_output_pkg);
} catch (Exception ex)
{
    Console.Error.WriteLine("Failed to create output PKG file.");
    Console.Error.WriteLine(ex);
    Environment.ExitCode = -4;
    return;
}


// tell the console what we're doing
Console.WriteLine("Building RB3DX PKG file for PS3...");
Console.WriteLine("Output path: " + arg_output_pkg);
Console.WriteLine("Content ID: " + content_id);
Console.WriteLine("Title ID: " + title_id);
Console.WriteLine("Package Version: " + title_version);
Console.WriteLine();

// build an SFO file
ParamSFO sfo = new();
sfo.AddValue("APP_VER", title_version, 0x8);
sfo.AddValue("CATEGORY", "GD", 0x4); // Game Data
sfo.AddValue("PARENTAL_LEVEL", 5); // PEGI 12
sfo.AddValue("PS3_SYSTEM_VER", "03.6000", 0x8);
sfo.AddValue("TITLE", title_name, 0x80);
sfo.AddValue("TITLE_ID", title_id, 0x10);
sfo.AddValue("VERSION", "01.00", 0x8);
// write it out to a memory stream
MemoryStream sfoStream = new();
sfo.Write(sfoStream);
// create a pkg file entry for it, to be added when building the pkg later
PKGFile paramSfoFile = new("PARAM.SFO", PKGFileFlags.Overwrites | PKGFileFlags.NPDRM |
                           PKGFileFlags.EDAT, sfoStream);

// start building the pkg
PKGWriter pkg = new(content_id);
pkg.SetDRMType(NPDRMType.Free);
pkg.SetContentType(PKGContentType.GameData);
pkg.SetFlags(PKGFlags.EBOOT | PKGFlags.CumulativePatch | PKGFlags.RenameDirectory |
             PKGFlags.DiscBinded | PKGFlags.Unknown_0x8);

// enumerate through all our input files
EnumerationOptions dirEnumOptions = new()
{
    RecurseSubdirectories = true,
    MaxRecursionDepth = 5,
    AttributesToSkip = FileAttributes.System
};
Console.WriteLine("Reading input files...");
foreach (string file in Directory.EnumerateFileSystemEntries(arg_content_path, "*", dirEnumOptions))
{
    // hack to just remove the content folder name and swap path seperators
    string filename = file.Replace(arg_content_path, "").Replace('\\', '/');
    // hack to remove leading slash from filename
    if (filename[0] == '/')
        filename = filename.Substring(1);
    if (Directory.Exists(file))
    {
        Console.WriteLine($"Adding directory \"{filename}\" : {file}");
        PKGFile dir = new(filename, PKGFileFlags.Directory | PKGFileFlags.Overwrites);
        pkg.AddFile(dir);
    }
    else if (File.Exists(file))
    {
        if (filename != "PARAM.SFO")
        {
            Console.WriteLine($"Adding file \"{filename}\" : {file}");
            FileStream fs = File.Open(file, FileMode.Open, FileAccess.Read);
            // set pkg flags based on type, eboots require npdrm
            PKGFileFlags flags = PKGFileFlags.Overwrites | PKGFileFlags.NPDRM;
            if (filename.EndsWith(".BIN") || filename.EndsWith(".self"))
                flags |= PKGFileFlags.SELF;
            else
                flags |= PKGFileFlags.EDAT;
            PKGFile file1 = new(filename, flags, fs);
            pkg.AddFile(file1);
        } else
        {
            Console.WriteLine($"Adding file \"{filename}\" from memory.");
            pkg.AddFile(paramSfoFile);
        }
    }
}

Console.WriteLine();
Console.WriteLine("Writing PKG file...");
pkg.WritePKG(out_pkg);
