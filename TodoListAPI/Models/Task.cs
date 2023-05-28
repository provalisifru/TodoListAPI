using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace TodoList.Models;
public partial class Task {
    public Guid TaskId { get; set; }

    public string TaskName { get; set; } = null!;

    public string TaskCategory { get; set; } = null!;

    public string TaskDescription { get; set; } = null!;

    public sbyte IsCompleted { get; set; }

    public Guid UserId { get; set; }

}
