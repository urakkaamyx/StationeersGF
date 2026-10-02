using System.Text;
namespace StationeersModCreator.Core.Services;
public static class SlotKeyHash {
 public static int Hash(string key){uint crc=0xffffffff;foreach(var value in Encoding.UTF8.GetBytes(key)){crc^=value;for(var i=0;i<8;i++)crc=(crc>>1)^((crc&1)!=0?0xedb88320u:0);}return unchecked((int)~crc);}
}
