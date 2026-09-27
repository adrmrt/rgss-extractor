using System.Windows.Forms;

namespace RgssExtractor.Tests;

public class MainFormTests
{
    private static TreeNode File(string name) => new(name) { Tag = new Entry { Name = name } };

    private static TreeNode Folder(string name, params TreeNode[] children)
    {
        var node = new TreeNode(name);
        node.Nodes.AddRange(children);
        return node;
    }

    [Fact]
    public void CollectEntriesReturnsEveryFileUnderFolderOnce()
    {
        var graphics = Folder("Graphics",
            Folder("Pictures", File("a.png"), File("b.png")),
            File("c.png"));

        var names = MainForm.CollectEntries(graphics).Select(e => e.Name);

        Assert.Equal(["a.png", "b.png", "c.png"], names);
    }

    [Fact]
    public void CollectEntriesReturnsSingleFile()
    {
        var names = MainForm.CollectEntries(File("a.png")).Select(e => e.Name);

        Assert.Equal(["a.png"], names);
    }
}
