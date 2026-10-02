using Avalonia.Media.Imaging;
using StationeersModCreator.Core.Models;
using StationeersModCreator.Desktop.Interfaces;
namespace StationeersModCreator.Desktop.ViewModels;
public sealed class InventorySlotCardViewModel {
 public InventorySlotDefinition Slot{get;}public string ParentPath{get;}public string? ItemPath{get;}public string? PrefabName{get;}public string Label{get;}public string Caption{get;}public string Detail{get;}public Bitmap? Image{get;}public bool IsSpeciesManaged=>Slot.TypeName=="Organ";public bool HasItem=>ItemPath is not null;public RelayCommand SelectCommand{get;}
 public InventorySlotCardViewModel(InventorySlotDefinition slot,string parentPath,InventoryPlacement? item,IBitmapProvider images,Action<InventorySlotCardViewModel> select,string detail){Slot=slot;ParentPath=parentPath;ItemPath=item?.ItemPath;PrefabName=item?.PrefabName;Label=(string.IsNullOrEmpty(slot.Key)?slot.TypeName=="None"?"Storage":slot.TypeName:slot.Key)+" · "+slot.Index;Caption=IsSpeciesManaged?"Species-managed organ":PrefabName is null?"Empty slot":DisplayName(PrefabName);Detail=detail;Image=PrefabName is null?null:images.Get(PrefabName+".png");SelectCommand=new(()=>select(this));}
 private static string DisplayName(string name)=>System.Text.RegularExpressions.Regex.Replace(System.Text.RegularExpressions.Regex.Replace(name,@"^(Item|Uniform)",""),@"(?<=[a-z0-9])(?=[A-Z])"," ");
}
