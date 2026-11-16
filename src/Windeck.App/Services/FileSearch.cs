using System.Data.OleDb;
namespace Windeck.Services;
internal static class FileSearch
{
    public static Task<List<(string Name,string Path)>> FindAsync(string query,CancellationToken token)=>Task.Run(()=>{
        token.ThrowIfCancellationRequested();
        // LIKE searches literal text only: escape quotes, wildcards and bracket patterns.
        string text=query.Trim();if(text.Length>120)text=text[..120];
        text=text.Replace("'","''").Replace("[","[[]").Replace("%","[%]").Replace("_","[_]");
        using var connection=new OleDbConnection("Provider=Search.CollatorDSO.1;Extended Properties=\"Application=Windows\"");connection.Open();
        using var command=connection.CreateCommand();command.CommandTimeout=3;
        command.CommandText=$"SELECT TOP 12 System.ItemNameDisplay, System.ItemUrl FROM SYSTEMINDEX WHERE System.ItemNameDisplay LIKE '%{text}%' AND System.Shell.OmitFromView != 'TRUE' ORDER BY System.DateModified DESC";
        using var reader=command.ExecuteReader();var results=new List<(string,string)>();
        while(reader!=null&&reader.Read()){token.ThrowIfCancellationRequested();string path=reader.GetValue(1)?.ToString()??"";if(Uri.TryCreate(path,UriKind.Absolute,out var uri)&&uri.IsFile)results.Add((reader.GetValue(0)?.ToString()??"Arquivo",uri.LocalPath));}
        return results;
    },token);
}
