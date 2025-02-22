using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace ECommerce.Hubs
{
    public class OrderHub : Hub
    {
        /// <summary>
        /// Yeni bir istemci bağlandığında çalışır.
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            // Bağlantı kurulduğunda istemciye mesaj gönder
            await Clients.Caller.SendAsync("ReceiveMessage", "SignalR bağlantısı başarılı.");
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Sipariş durumunu bağlı tüm istemcilere iletir.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="status">Siparişin yeni durumu</param>
        public async Task UpdateOrderStatus(int orderId, string status)
        {
            // Tüm istemcilere sipariş durumu güncellemesi gönder
            await Clients.All.SendAsync("OrderStatus", orderId, status);
        }

        /// <summary>
        /// Loglama veya kritik süreç değişimlerinde kullanılır.
        /// </summary>
        /// <param name="message">Log mesajı</param>
        public async Task LogMessage(string message)
        {
            await Clients.All.SendAsync("LogUpdate", message);
        }

        /// <summary>
        /// Gruplar halinde iletişim kurmayı sağlar.
        /// </summary>
        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            await Clients.Group(groupName).SendAsync("ReceiveMessage", $"{Context.ConnectionId} gruba katıldı: {groupName}");
        }

        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            await Clients.Group(groupName).SendAsync("ReceiveMessage", $"{Context.ConnectionId} gruptan ayrıldı: {groupName}");
        }


        /// <summary>
        /// Güncellenen sipariş listesini istemcilere gönderir.
        /// </summary>
        public async Task UpdateOrderList(object orders)
        {
            await Clients.All.SendAsync("UpdateOrderList", orders);
        }
    }
}
