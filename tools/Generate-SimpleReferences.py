"""Build authored 1.1 topic references and presentation scopes; no downloads or old-file writes.
Run from any directory. Existing 1.1 output must match exactly (immutable package versions).
"""
from pathlib import Path
import copy
import hashlib
import json

ROOT = Path(__file__).resolve().parents[1] / 'resources' / 'knowledge-packages'
VERSION = '1.1.0'


def read(key):
    return json.loads((ROOT / ('org.yanji.reference.' + key) / '1.0.0' / 'package.yanji-knowledge.json').read_text(encoding='utf-8-sig'))


def emit(path, value, immutable=False):
    content = (json.dumps(value, ensure_ascii=False, indent=2) + '\n').encode('utf-8')
    if path.exists() and immutable and path.read_bytes() != content:
        raise SystemExit(f'Refusing to replace a different immutable version: {path}')
    path.parent.mkdir(parents=True, exist_ok=True)
    if not path.exists() or path.read_bytes() != content:
        path.write_bytes(content)


class Reference:
    def __init__(self, key):
        self.p = copy.deepcopy(read(key))
        self.key = key
        self.p['PackageVersion'] = VERSION
        self.p['Description'] = ('YanJi 原创主题参考目录；不是官方考试大纲，也不是2028官方范围。'
            '用于规划、计时与独立进度记录；不保证逐项完整或预测考试题型。'
            '目录与简短说明原创，不附教材、试题、答案或讲义；请按报考年度大纲与院校要求核对。')
        self.scopes = {n['Id']: ['common'] for n in self.p['Nodes']}

    def node(self, id):
        return next(n for n in self.p['Nodes'] if n['Id'] == id)

    def add(self, suffix, name, parent=None, points='', scope='common', description=''):
        id = self.key + '.' + suffix
        siblings = [n['Order'] for n in self.p['Nodes'] if n['ParentId'] == parent]
        if any(n['Id'] == id for n in self.p['Nodes']):
            raise ValueError('Duplicate ID: ' + id)
        self.p['Nodes'].append(dict(Id=id, SubjectKey=self.p['Subjects'][0]['Key'], ParentId=parent,
            Kind='Chapter', Name=name, Description=description, Order=max(siblings, default=-1) + 1))
        self.scopes[id] = [scope]
        for i, point in enumerate(points.split('|') if points else []):
            pid = f'{id}.p{i}'
            self.p['Nodes'].append(dict(Id=pid, SubjectKey=self.p['Subjects'][0]['Key'], ParentId=id,
                Kind='KnowledgePoint', Name=point, Description='', Order=i))
            self.scopes[pid] = [scope]
        return id

    def points(self, parent, points, scope='common'):
        for point in points.split('|'):
            order = max((n['Order'] for n in self.p['Nodes'] if n['ParentId'] == parent), default=-1) + 1
            id = f'{parent}.v11p{order}'
            self.p['Nodes'].append(dict(Id=id, SubjectKey=self.p['Subjects'][0]['Key'], ParentId=parent,
                Kind='KnowledgePoint', Name=point, Description='', Order=order))
            self.scopes[id] = [scope]

    def finish(self):
        p = self.p
        del p['ContentHash']
        canonical = json.dumps(p, ensure_ascii=False, sort_keys=True, separators=(',', ':')).encode('utf-8')
        p['ContentHash'] = hashlib.sha256(canonical).hexdigest().upper()
        path = ROOT / p['PackageId'] / VERSION / 'package.yanji-knowledge.json'
        emit(path, p, immutable=True)
        entry = dict(PackageId=p['PackageId'], Name=p['Name'], Nodes=len(p['Nodes']),
            Path=path.relative_to(ROOT).as_posix(), ContentHash=p['ContentHash'])
        scopes = [dict(PackageId=p['PackageId'], ExternalNodeId=n['Id'], ParentId=n['ParentId'], Variants=self.scopes[n['Id']]) for n in p['Nodes']]
        return entry, scopes


def math(key):
    r = Reference(key)
    # The second variant receives the first variant's extension IDs in its own existing namespace.
    # Historical package identities remain independent, with no cross-package merging.
    if key == 'math2':
        for n in read('math1')['Nodes']:
            if n['Id'].startswith('math1.r2'):
                n = copy.deepcopy(n)
                n['Id'] = n['Id'].replace('math1.', 'math2.', 1)
                if n['ParentId']:
                    n['ParentId'] = n['ParentId'].replace('math1.', 'math2.', 1)
                r.p['Nodes'].append(n)
                r.scopes[n['Id']] = ['math1']
    for n in r.p['Nodes']:
        if n['Id'].startswith(key + '.r2'):
            r.scopes[n['Id']] = ['math1']
    r.node(key + '.r2')['Name'] = '数学一扩展主题'
    r.points(key + '.r0.c0', '数列极限|极限运算法则|等价无穷小|函数间断点|闭区间连续函数性质')
    r.points(key + '.r0.c1', '复合函数与隐函数求导|高阶导数|洛必达法则|单调性与极值|凹凸性与拐点|曲率与微分近似')
    r.points(key + '.r0.c2', '换元与分部积分|变上限定积分|反常积分|面积、体积与弧长|定积分的几何与物理应用')
    r.points(key + '.r0.c3', '多元函数极限与连续|复合函数与隐函数偏导|条件极值|二重积分计算与换序')
    r.points(key + '.r0.c4', '可分离变量方程|一阶线性方程|可降阶方程|二阶常系数非齐次方程|微分方程应用')
    r.points(key + '.r1.c0', '行列式展开与计算|逆矩阵|初等变换|分块矩阵')
    r.points(key + '.r1.c1', '向量组的秩|极大线性无关组|齐次方程组解空间|非齐次方程组通解')
    r.points(key + '.r1.c2', '实对称矩阵|正交变换|二次型标准化|正定二次型')
    ext = key + '.r2'
    r.add('v11.space', '空间解析几何与向量代数', ext,
          '向量运算|平面与直线|曲面与空间曲线', 'math1')
    r.add('v11.multiple', '多元积分扩展', ext,
          '三重积分|第一类与第二类曲线积分|格林公式|第一类与第二类曲面积分|高斯公式与斯托克斯公式', 'math1')
    r.add('v11.series', '无穷级数', ext,
          '数项级数与收敛判别|绝对与条件收敛|幂级数与收敛域|泰勒展开|傅里叶级数', 'math1')
    prob = r.add('v11.probability', '概率论与数理统计', ext, scope='math1')
    for suffix, title, points in [
        ('events', '事件与概率', '事件关系与运算|条件概率|全概率与贝叶斯公式|独立性'),
        ('variables', '随机变量与分布', '离散型与连续型随机变量|分布函数与概率密度|常见概率分布|随机变量函数的分布'),
        ('joint', '多维随机变量', '联合与边缘分布|条件分布|随机变量独立性|二维随机变量函数的分布'),
        ('moments', '数字特征与极限定理', '数学期望与方差|协方差与相关系数|大数定律|中心极限定理'),
        ('inference', '统计推断', '总体、样本与统计量|抽样分布|矩估计与最大似然估计|估计量评价|区间估计|假设检验')]:
        r.add('v11.' + suffix, title, prob, points, 'math1')
    return r


def english(key):
    r = Reference(key)
    foundation = key + '.r0'
    r.add('v11.vocabulary', '词汇与语境', foundation, '核心词复习|一词多义与熟词僻义|构词与词族|固定搭配|同义替换与语义关系')
    r.add('v11.grammar', '语法基础', foundation, '词类与句子成分|时态与语态|非谓语动词|定语、名词性与状语从句|虚拟语气|倒装、强调与省略')
    r.add('v11.sentences', '长难句分析', foundation, '主干定位|修饰成分与插入语|并列与嵌套结构|指代与逻辑衔接')
    training = key + '.r1'
    r.add('v11.cloze', '完形填空（英语知识运用）', training, '词义与搭配辨析|语法线索|上下文语义|篇章逻辑|选项比较与复盘')
    r.points(key + '.r1.c0', '细节定位|语境猜词|作者态度|例证作用|干扰项辨析|阅读错因整理')
    new = r.add('v11.new-reading', '新题型', training)
    r.add('v11.headings', '小标题与概括句匹配', new, '段落主旨提炼|标题层级与范围|概括句对应')
    r.add('v11.ordering', '段落排序', new, '首尾线索|指代链|时间与论证顺序', 'english1')
    r.add('v11.insertion', '选句填空', new, '空前空后衔接|句段逻辑|补全篇章结构', 'english1')
    r.add('v11.matching', '多项匹配（信息对应）', new, '人物与观点对应|信息定位|同义表述核对', 'english2')
    r.points(key + '.r1.c1', '语义准确性|词序调整与表达自然|翻译检查与修订')
    r.add('v11.sentence-translation', '语篇划线句翻译', key + '.r1.c1', '句子在语篇中的含义|复杂结构的汉语重组', 'english1')
    r.add('v11.paragraph-translation', '段落翻译', key + '.r1.c1', '段落语义连贯|代词指代与衔接', 'english2')
    r.add('v11.practical', '应用文写作', key + '.r1.c2', '书信与通知|写作目的与对象|信息完整与格式|语气与语言检查')
    r.add('v11.picture', '图画作文', key + '.r1.c2', '图画描述|寓意与论点|论证展开', 'english1',
          '学习训练侧重点；不表示英语一只允许图画提示，须核对当年要求。')
    r.add('v11.chart', '图表作文', key + '.r1.c2', '趋势与对比描述|数据概括|原因与启示', 'english2',
          '学习训练侧重点；不表示英语二只允许图表提示，须核对当年要求。')
    r.add('v11.revision', '复习与模考复盘', points='词汇回收|阅读错因|翻译修订|写作反馈|阶段复盘与下一步')
    return r


def computer():
    r = Reference('408')
    r.add('v11.strings', '串与模式匹配', '408.r0', '字符串存储与基本操作|朴素模式匹配|KMP模式匹配|前缀函数与next数组|匹配失败时的回退')
    r.points('408.r0.c3', '二叉树性质与存储|二叉排序树操作|并查集')
    r.points('408.r0.c4', '图的存储转换|连通性与生成树|有向无环图应用')
    r.points('408.r1.c1', '整数与浮点数转换|加法器与运算部件')
    r.points('408.r1.c2', '存储器交叉编址|Cache写策略与一致性基础|地址翻译与TLB')
    r.points('408.r1.c4', '异常与中断分类|多处理器基本概念')
    r.points('408.r2.c1', '进程间通信|调度指标与典型算法')
    r.points('408.r2.c4', '文件打开与关闭|文件系统可靠性基础')
    r.points('408.r2.c5', '固态存储与设备特性|I/O软件层次')
    r.points('408.r3.c2', '随机接入与轮询|VLAN基础')
    r.points('408.r3.c3', 'IP数据报与分片|NAT与DHCP|SDN基础')
    r.points('408.r3.c5', 'HTTPS与安全基础|应用层交互过程分析')
    return r


def main():
    originals = {p: p.read_bytes() for p in ROOT.glob('*/1.0.0/*.json')}
    entries = []
    scopes = []
    for r in [computer(), math('math1'), math('math2'), english('english1'), english('english2')]:
        entry, members = r.finish()
        entries.append(entry)
        if r.key != '408':
            scopes.extend(members)
    for key in ['politics', 'blank']:
        p = read(key)
        entries.append(dict(PackageId=p['PackageId'], Name=p['Name'], Nodes=len(p['Nodes']),
            Path=f"{p['PackageId']}/1.0.0/package.yanji-knowledge.json", ContentHash=p['ContentHash']))
    emit(ROOT / 'index.json', entries)
    emit(ROOT / 'reference-scopes.json', dict(SchemaVersion=1, Entries=scopes))
    assert all(p.read_bytes() == raw for p, raw in originals.items()), 'Historical bytes changed'
    print(json.dumps(dict(Latest=entries, ScopeEntries=len(scopes), HistoricalFilesUnchanged=len(originals)), ensure_ascii=False, indent=2))


if __name__ == '__main__':
    main()
