namespace linear_programming_solver.Models;

public class LinearProgramModel
{
    public string FileName { get; set; } = "";
    public string FileContent { get; set; } = "";
    public bool IsLoaded { get; set; } = false;
}