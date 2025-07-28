using System.Diagnostics;
using Audio.Processing.Pipelines;

var startNode = new Node<double, double>(new AddOne()); //2
var secondNode = new Node<double, double>(new MultiplyByTwo()); //4
var thirdNode = new Node<double, double>(new MultiplyByTwo()); //8
var fourthNode = new Node<double, double>(new MinusOne()); //7
var fifthNode = new Node<double, double>(new HalfInput()); //3.5
var sixthNode = new Node<double, int>(new ToInteger()); // 3

var multiNode = new MultiNode<double, double>(new Average());
var multiNodeTwo = new MultiNode<double, double>(new Average());

var outputNode = new Node<int, double>(new ToDouble());
var outputNodeTwo = new Node<double, double>(new PassThrough<double>());
var outputNodeThree = new Node<double, double>(new PassThrough<double>());
var outputNodeFour = new Node<double, double>(new PassThrough<double>());


startNode.Connect(secondNode);
secondNode.Connect(thirdNode);
thirdNode.Connect(fourthNode);
fourthNode.Connect(fifthNode);
fifthNode.Connect(sixthNode);
sixthNode.Connect(outputNode);

secondNode.Connect(outputNodeTwo);

startNode.Connect(multiNode);
secondNode.Connect(multiNode);
thirdNode.Connect(multiNode);
fourthNode.Connect(multiNode);
fifthNode.Connect(multiNode);

multiNode.Connect(outputNodeThree);

outputNode.Connect(multiNodeTwo);
outputNodeTwo.Connect(multiNodeTwo);
outputNodeThree.Connect(multiNodeTwo);

multiNodeTwo.Connect(outputNodeFour);

// Run the entire pipeline
startNode.Consume(1);

var output = outputNode.GetData();
Debug.Assert(output == 3);
Console.WriteLine(output);

var outputTwo = outputNodeTwo.GetData();
Debug.Assert(outputTwo == 4);
Console.WriteLine(outputTwo);

var outputThree = outputNodeThree.GetData();
Debug.Assert(outputThree == 4.9);
Console.WriteLine(outputThree);

var outputFour = outputNodeFour.GetData();
Debug.Assert(outputFour == 3.966666666666667);
Console.WriteLine(outputFour);


var inputNodeOne = new Node<bool, bool>(new PassThrough<bool>());
var inputNodeTwo = new Node<double, double>(new PassThrough<double>());

var compositeNode = new CompositeNode<bool, double, double>(new AddTenWhenInputOn());

var outputNodeFive = new Node<double, double>();

inputNodeOne.ConnectToObjectSink(compositeNode);
inputNodeTwo.ConnectToObjectSink(compositeNode);

compositeNode.Connect(outputNodeFive);

inputNodeOne.Consume(true);
inputNodeTwo.Consume(10);

var outputFive = outputNodeFive.GetData();
Debug.Assert(outputFive == 20);
Console.WriteLine(outputFive);

var timeNode = new Node<int, int>(new PassThrough<int>());

var aFrequencyNode = new InputNode<double>();
var bFrequencyNode = new InputNode<double>();
var cFrequencyNode = new InputNode<double>();
var dFrequencyNode = new InputNode<double>();
var eFrequencyNode = new InputNode<double>();
var fFrequencyNode = new InputNode<double>();
var gFrequencyNode = new InputNode<double>();

var aOscillatorNode = new CompositeNode<int, double, double>(new Oscillator());
var bOscillatorNode = new CompositeNode<int, double, double>(new Oscillator());
var cOscillatorNode = new CompositeNode<int, double, double>(new Oscillator());
var dOscillatorNode = new CompositeNode<int, double, double>(new Oscillator());
var eOscillatorNode = new CompositeNode<int, double, double>(new Oscillator());
var fOscillatorNode = new CompositeNode<int, double, double>(new Oscillator());
var gOscillatorNode = new CompositeNode<int, double, double>(new Oscillator());

var aInputNode = new InputNode<bool>();
var bInputNode = new InputNode<bool>();
var cInputNode = new InputNode<bool>();
var dInputNode = new InputNode<bool>();
var eInputNode = new InputNode<bool>();
var fInputNode = new InputNode<bool>();
var gInputNode = new InputNode<bool>();

var aDynamicsNode = new CompositeNode<bool, double, double>(new AdsrSimple());
var bDynamicsNode = new CompositeNode<bool, double, double>(new AdsrSimple());
var cDynamicsNode = new CompositeNode<bool, double, double>(new AdsrSimple());
var dDynamicsNode = new CompositeNode<bool, double, double>(new AdsrSimple());
var eDynamicsNode = new CompositeNode<bool, double, double>(new AdsrSimple());
var fDynamicsNode = new CompositeNode<bool, double, double>(new AdsrSimple());
var gDynamicsNode = new CompositeNode<bool, double, double>(new AdsrSimple());

var mixerNode = new MultiNode<double, double>(new Average());
var outputNodeSix = new Node<double, double>();

timeNode.ConnectToObjectSink(aOscillatorNode);
timeNode.ConnectToObjectSink(bOscillatorNode);
timeNode.ConnectToObjectSink(cOscillatorNode);
timeNode.ConnectToObjectSink(dOscillatorNode);
timeNode.ConnectToObjectSink(eOscillatorNode);
timeNode.ConnectToObjectSink(fOscillatorNode);
timeNode.ConnectToObjectSink(gOscillatorNode);

aFrequencyNode.ConnectToObjectSink(aOscillatorNode);
bFrequencyNode.ConnectToObjectSink(bOscillatorNode);
cFrequencyNode.ConnectToObjectSink(cOscillatorNode);
dFrequencyNode.ConnectToObjectSink(dOscillatorNode);
eFrequencyNode.ConnectToObjectSink(eOscillatorNode);
fFrequencyNode.ConnectToObjectSink(fOscillatorNode);
gFrequencyNode.ConnectToObjectSink(gOscillatorNode);

aOscillatorNode.ConnectToObjectSink(aDynamicsNode);
bOscillatorNode.ConnectToObjectSink(bDynamicsNode);
cOscillatorNode.ConnectToObjectSink(cDynamicsNode);
dOscillatorNode.ConnectToObjectSink(dDynamicsNode);
eOscillatorNode.ConnectToObjectSink(eDynamicsNode);
fOscillatorNode.ConnectToObjectSink(fDynamicsNode);
gOscillatorNode.ConnectToObjectSink(gDynamicsNode);

aInputNode.ConnectToObjectSink(aDynamicsNode);
bInputNode.ConnectToObjectSink(bDynamicsNode);
cInputNode.ConnectToObjectSink(cDynamicsNode);
dInputNode.ConnectToObjectSink(dDynamicsNode);
eInputNode.ConnectToObjectSink(eDynamicsNode);
fInputNode.ConnectToObjectSink(fDynamicsNode);
gInputNode.ConnectToObjectSink(gDynamicsNode);

aDynamicsNode.Connect(mixerNode);
bDynamicsNode.Connect(mixerNode);
cDynamicsNode.Connect(mixerNode);
dDynamicsNode.Connect(mixerNode);
eDynamicsNode.Connect(mixerNode);
fDynamicsNode.Connect(mixerNode);
gDynamicsNode.Connect(mixerNode);

mixerNode.Connect(outputNodeSix);

for (int i = 0; i < 338; i++)
{
    timeNode.Consume(i);
    
    aFrequencyNode.Consume(110.00);   // A2
    bFrequencyNode.Consume(123.47);   // B2
    cFrequencyNode.Consume(130.81);   // C3
    dFrequencyNode.Consume(146.83);   // D3
    eFrequencyNode.Consume(164.81);   // E3
    fFrequencyNode.Consume(174.61);   // F3
    gFrequencyNode.Consume(196.00);   // G3
    
    var index = (i / 50) % 7;

    aInputNode.Consume(index == 0);
    bInputNode.Consume(index == 1);
    cInputNode.Consume(index == 2);
    dInputNode.Consume(index == 3);
    eInputNode.Consume(index == 4);
    fInputNode.Consume(index == 5);
    gInputNode.Consume(index == 6);
    
    var outputSix = outputNodeSix.GetData();
    Console.WriteLine(outputSix);
}


var intInputNodeOne = new InputNode<int>();
var intInputNodeTwo = new InputNode<int>();

var doubleInputNodeOne = new InputNode<double>();
var doubleInputNodeTwo = new InputNode<double>();


var multiCompositeNode = new MultiCompositeNode<int, double, double>(new MultiCompositeProcess());

var outputNodeSeven = new Node<double, double>();

intInputNodeOne.ConnectToObjectSink(multiCompositeNode);
intInputNodeTwo.ConnectToObjectSink(multiCompositeNode);
doubleInputNodeOne.ConnectToObjectSink(multiCompositeNode);
doubleInputNodeTwo.ConnectToObjectSink(multiCompositeNode);
multiCompositeNode.Connect(outputNodeSeven);

intInputNodeOne.Consume(1);
intInputNodeTwo.Consume(2);
doubleInputNodeOne.Consume(3.0);
doubleInputNodeTwo.Consume(4.0);

var outputSeven = outputNodeSeven.GetData();
Debug.Assert(outputSeven == 11);
Console.WriteLine(outputSeven);