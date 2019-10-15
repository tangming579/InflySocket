## System.IO.Pipelines: .NET高性能IO

> 它非常适合那些在IO代码中复杂却普遍的痛点；使我们可以替换掉那些丑陋的封装(kludge)、变通(workaround)或妥协(compromise)——用一个在框架中设计优雅的专门的解决方案。

### 什么是Pipelines

它们实现对一个二进制流解耦、重叠(overlapped)的读写访问，包括缓冲区管理(池化，回收)，线程感知，丰富的积压控制，和通过背压达到的溢出保护——所有这些都基于一个围绕非连续内存设计的 API

PipeReader有两个核心API `ReadAsync`和`AdvanceTo`。`ReadAsync`获取Pipe数据，`AdvanceTo`告诉PipeReader不再需要这些缓冲区，以便可以丢弃它们（例如返回到底层缓冲池）。

### 解决的问题

我们在编写网络程序的时候，经常会进行如下操作：

1. 申请一个缓冲区
2. 从数据源中读入数据至缓冲区
3. 解析缓冲区的数据
4. 重复第2步

**数据读不全：**

可能不能在一次read操作中读入所有需要的数据，因此需要在缓冲区中维护一个游标，记录下次读取操作的起始位置，这个游标带了了不小的复杂度：

- 从缓冲区读数据时，要根据游标计算缓冲区起始写位置，以及剩余空间大小。增加了读数据的复杂度。
- 解析数据也是复用这个缓冲区的，解析的时候也要判断游标起始位置，剩余空间大小。同时增加了解析数据的复杂度。
- 解析玩了后还要移动游标，重新标记缓冲区起始位置，再次增加了复杂度。

**缓冲区容量有限：**

由于缓冲区有限，可能申请的缓冲区不够用，需要引入动态缓冲区。这也大幅加大了代码的复杂度。

- 如果每次都申请更大的内存，一方面带来的内存申请释放开销，另一方面需要将原来的数据移动，并更新游标，带来更复杂的逻辑。
- 如果靠多段的内存组成一个逻辑整理，数据的读写方式都比较复杂。
- 使用完后的内存要释放，如果需要更高的效率还要维持一个内存池。

**读和用没有分离**

我们的业务本身只关心使用操作，但读和用操作没有分离，复杂的都操作导致用操作也变得复杂，并且严重干扰业务逻辑。

### Pipelines使用

System.IO.Pipelines（需要在Nuget上安装），用于解决这些痛点。它主要包含一个Pipe对象，它有一个Writer属性和Reader属性。

```c#
var pipe   = new Pipe();
var writer = pipe.Writer;
var reader = pipe.Reader;
```

**Writer对象**

Writer对象用于从数据源读取数据，将数据写入管道中；它对应业务中的"读"操作。

```c#
var content = Encoding.Default.GetBytes("hello world");
var data    = new Memory<byte>(content);
var result  = await writer.WriteAsync(data);
```

另外，它也有一种使用Pipe申请Memory的方式

```c#
var buffer = writer.GetMemory(512);
content.CopyTo(buffer);
writer.Advance(content.Length);
var result = await writer.FlushAsync();
```

**Reader对象**

Reader对象用于从管道中获取数据源，它对应业务中的"用"操作。

首先获取管道的缓冲区：

```c#
var result = await reader.ReadAsync();
var buffer = result.Buffer;
```

这个Buffer是一个ReadOnlySequence<byte>对象，它是一个相当好的动态内存对象，并且相当高效。它本身由多段Memory<byte>组成，查看Memory段的方法有：

- IsSingleSegment： 判断是否只有一段Memory<byte>
- First： 获取第一段Memory<byte>
- GetEnumerator: 获取分段的Memory<byte>

它从逻辑上也可以看成一段连续的Memory<byte>，也有类似的方法：

- Length： 整个数据缓冲区长度
- Slice： 分割缓冲区
- CopyTo:　将内容复制到Span中
- ToArray： 将内容复制到byte[]中

另外，它还有一个类似游标的位置对象SequencePosition，可以从其Position相关函数中使用。

这个缓冲区解决了"数据读不够"的问题，一次读取的不够下次可以接着读，不用缓冲区的动态分配，高效的内存管理方式带来了良好的性能，好用的接口是我们能更关注业务。

获取到缓冲区后，就是使用缓冲区的数据

```c#
var data = buffer.ToArray();
```

使用完后，告诉PIPE当前使用了多少数据，下次接着从结束位置后读起

```c#
reader.AdvanceTo(buffer.GetPosition(4));
```

这是一个相当实用的设计，它解决了"读了就得用"的问题，不仅可以将不用的数据下次再使用，还可以实现Peek的操作，只读但不改变游标。

**交互**

除了"读"和"用"操作外，它们之间还需要一些交互，例如：

1. 读过程中数据源不可用，需要停止使用
2. 使用过程中业务结束，需要中止数据源。

Reader和Writer都有一个Complete函数，用于通知结束：

```c#
reader.Complete();
writer.Complete();
```

在Writer写入和Reader读取时，会获得一个结果

```c#
FlushResult result = await writer.FlushAsync();
ReadResult result = await reader.ReadAsync();
```

它们都有一个IsComplete属性，可以根据它是否为true判断是否已经结束了读和写的操作。

**取消**

在写入和读取的时候，也可以传入一个CancellationToken，用于取消相应的操作。

```c#
writer.FlushAsync(CancellationToken.None);
reader.ReadAsync(CancellationToken.None);
```

