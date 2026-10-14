"""Generate YanJi-authored topic-only reference data; never downloads source material."""
from pathlib import Path
import json, hashlib

ROOT = Path(__file__).resolve().parents[1] / 'resources' / 'knowledge-packages'
N = 'https://yankao.neea.edu.cn/html1/category/1509/6235-2.htm'
def write(key, name, subject, groups, sources, variant=None, note='基础参考，须自行核对报考年度大纲与院校要求。'):
    nodes=[]
    for ri,(root, chapters) in enumerate(groups):
        rid=f'{key}.r{ri}'
        nodes.append(dict(Id=rid,SubjectKey=subject,ParentId=None,Kind='Chapter',Name=root,Description='',Order=ri))
        for ci,(title, points) in enumerate(chapters):
            cid=f'{rid}.c{ci}'
            nodes.append(dict(Id=cid,SubjectKey=subject,ParentId=rid,Kind='Chapter',Name=title,Description='',Order=ci))
            for pi,point in enumerate(points.split('|') if points else []):
                nodes.append(dict(Id=f'{cid}.p{pi}',SubjectKey=subject,ParentId=cid,Kind='KnowledgePoint',Name=point,Description='',Order=pi))
    p=dict(PackageId=f'org.yanji.reference.{key}',PackageVersion='1.0.0',SchemaVersion=1,Name=name,
           Description=f'YanJi 原创参考知识目录；不是官方考试大纲。{note}来源仅供追溯，不附教材、试题或讲义内容。',
           Maintainer='YanJi 参考目录维护',ExamKey='cn-postgraduate',AdmissionYears=[],
           Sources=[dict(Id=sid,Title=title,Url=url) for sid,title,url in sources],
           License=dict(Id='CC0-1.0',Text='本包原创目录结构与元数据采用 CC0-1.0；外部来源的内容与许可不包含在授权中，未复制其正文或题目。'),
           Subjects=[dict(Key=subject,Name=subject,Variant=variant)],Nodes=nodes)
    raw=json.dumps(p,ensure_ascii=False,sort_keys=True,separators=(',',':')).encode('utf-8')
    p['ContentHash']=hashlib.sha256(raw).hexdigest().upper()
    directory=ROOT/p['PackageId']/p['PackageVersion']; directory.mkdir(parents=True,exist_ok=True)
    file=directory/'package.yanji-knowledge.json'
    if file.exists(): raise SystemExit(f'Refusing to overwrite existing package: {file}')
    file.write_text(json.dumps(p,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    return dict(PackageId=p['PackageId'],Name=name,Nodes=len(nodes),Path=str(file.relative_to(ROOT)),ContentHash=p['ContentHash'])

def main():
    all=[]
    all.append(write('408','408 · 四科参考知识目录','408',[
      ('数据结构',[
        ('算法分析','时间复杂度|空间复杂度|递归与迭代'),
        ('线性表','顺序存储|单链表与双链表|循环链表|线性表操作与应用'),
        ('栈、队列与数组','顺序栈与链栈|队列与循环队列|表达式求值|矩阵压缩存储'),
        ('树与二叉树','树的表示|二叉树遍历|线索二叉树|树与森林转换|哈夫曼树'),
        ('图','邻接矩阵与邻接表|深度与广度优先遍历|最小生成树|最短路径|拓扑排序与关键路径'),
        ('查找','顺序与折半查找|二叉搜索树|平衡树|B树与B+树|散列表'),
        ('排序','插入与希尔排序|交换排序|选择与堆排序|归并排序|基数排序|排序稳定性与性能')]),
      ('计算机组成原理',[
        ('系统层次与性能','硬件与软件层次|指令执行基本过程|性能指标'),
        ('数据表示与运算','进制与编码|定点数与补码|浮点数|算术运算与溢出|逻辑运算'),
        ('存储系统','存储器层次|主存组织与扩展|高速缓存|映射与替换策略|虚拟存储基础'),
        ('指令系统','指令格式|寻址方式|指令功能|RISC与CISC'),
        ('处理器','数据通路|控制器|指令周期|中断响应|流水线与相关'),
        ('总线','总线结构|仲裁|传输与时序'),
        ('输入输出','设备与接口|程序查询|中断方式|DMA方式')]),
      ('操作系统',[
        ('操作系统基础','内核与用户态|中断、异常与系统调用|系统结构'),
        ('进程与线程','进程状态|线程模型|上下文切换|处理机调度'),
        ('同步与死锁','互斥与临界区|信号量|经典同步问题|死锁条件|预防、避免与检测'),
        ('内存管理','连续分配|分页与分段|地址转换|虚拟内存|页面置换|工作集与抖动'),
        ('文件系统','文件与目录|文件分配|空闲空间管理|文件系统结构|保护与共享'),
        ('输入输出管理','设备控制|缓冲|设备分配|磁盘调度')]),
      ('计算机网络',[
        ('网络体系','分层与协议|交换方式|时延、吞吐与带宽'),
        ('物理层','信号与编码|传输介质|复用|信道容量'),
        ('数据链路层','成帧与差错检测|流量控制|可靠传输|介质访问控制|以太网与交换机'),
        ('网络层','IPv4与子网|转发与路由|路由协议|ARP与ICMP|IPv6基础'),
        ('传输层','端口与多路复用|UDP|TCP连接管理|可靠传输与滑动窗口|拥塞控制'),
        ('应用层','DNS|HTTP|电子邮件|文件传输|应用协议比较')])
      ],[
        ('neea-index','教育考试院大纲索引：仅作历史考试范围入口',N),
        ('mit-algorithms','MIT 6.006 基础算法课程：主题参考','https://ocw.mit.edu/courses/6-006-introduction-to-algorithms-spring-2020/pages/syllabus/'),
        ('cmu-systems','CMU 15-213 系统课程：主题参考','https://www.cs.cmu.edu/~213/schedule.html'),
        ('mit-os','MIT 6.1810 操作系统课程：主题参考','https://pdos.csail.mit.edu/6.1810/2025/schedule.html'),
        ('stanford-network','Stanford CS144 网络课程：主题参考','https://cs144.github.io/')],note='覆盖四科常见主干主题，可用于安排和记录学习；不保证与2028或任一年度考试大纲逐项一致。'))
    calculus=[('极限与连续','函数与极限|无穷小与无穷大|连续性'),('微分学','导数与微分|中值定理|导数应用'),('积分学','不定积分|定积分|积分应用'),('多元函数','偏导与全微分|多元极值|二重积分'),('微分方程','一阶微分方程|高阶常系数方程')]
    algebra=[('矩阵与行列式','行列式|矩阵运算|矩阵的秩'),('向量与方程组','向量组|线性相关|线性方程组'),('特征值与二次型','特征值与特征向量|相似变换|二次型')]
    mathSources=[('neea-index','教育考试院大纲索引：不声明最新范围',N),('mit-math','MIT OCW 基础数学课程入口','https://ocw.mit.edu/collections/ocw-scholar/')]
    for key,label in [('math1','数学一'),('math2','数学二')]:
        groups=[('高等数学',calculus),('线性代数',algebra)]
        if key=='math1':groups.append(('补充基础主题', [('空间与级数','空间解析几何|曲线曲面积分|无穷级数'),('概率统计','随机事件|随机变量|数字特征|常见分布|大数定律与中心极限定理|参数估计')]))
        all.append(write(key,f'{label} · 基础参考目录','math',groups,mathSources,label))
    for key,label in [('english1','英语一'),('english2','英语二')]:
        all.append(write(key,f'{label} · 基础学习目录','english',[
            ('语言基础',[('词汇与语法','词汇复习|搭配与语境|句法与长句')]),
            ('阅读与表达',[('阅读训练','篇章结构|主旨与细节|推断与论证'),('翻译训练','句子理解|译文表达'),('写作训练','应用文|主题写作|修改与反馈')])], [('neea-index','教育考试院大纲索引：英语一二历史入口',N)],label,note='只提供语言学习框架；不声明题型、分值、词表和当年范围完整。'))
    all.append(write('politics','政治 · 基础参考目录','politics',[
        ('基础学习框架',[('理论与历史','马克思主义基本原理|中国近现代史|中国化时代化理论学习'),('法治与修养','思想道德|法治基础'),('时事与政策','自行维护年度材料|阶段整理与复习')])], [('neea-politics','教育考试院2017历史大纲入口；非当前年度说明','https://yankao.neea.edu.cn/html1/report/16103/1369-1.htm')],note='基础占位参考，当前政策和当年考试范围由用户依据合法材料维护；未宣称完整。'))
    all.append(write('blank','自定义 · 空白知识包','custom',[],[('yanji-original','YanJi 原创空白模板',None)],note='安装不产生节点；自行在知识目录添加内容。'))
    if (ROOT/'index.json').exists():raise SystemExit('Refusing to overwrite existing index')
    (ROOT/'index.json').write_text(json.dumps(all,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(all,ensure_ascii=False,indent=2))

if __name__=='__main__':main()
