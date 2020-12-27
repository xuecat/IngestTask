const React = require('react')
const Chart = require('../components/time-series-chart.jsx')
const CounterWidget = require('../components/counter-widget.jsx')
const SiloBreakdown = require('./silo-table.jsx')
const Panel = require('../components/panel.jsx')
const Page = require('../components/page.jsx')

const GrainGraph = props => {
  const values = []
  const timepoints = []
  Object.keys(props.stats).forEach(key => {
    values.push(props.stats[key])
    timepoints.push(props.stats[key].period)
  })

  if (!values.length) {
    return null
  }

  while (values.length < 100) {
    values.unshift({ count: 0, elapsedTime: 0, period: 0, exceptionCount: 0 })
    timepoints.unshift('')
  }

  return (
    <div>
      <h4>{props.grainMethod}</h4>
      <Chart
        timepoints={timepoints}
        series={[
          values.map(z => z.exceptionCount),
          values.map(z => z.count),
          values.map(z => (z.count === 0 ? 0 : z.elapsedTime / z.count))
        ]}
      />
    </div>
  )
}
const Taskinfo = props => {
  const values = props.stats
  const timepoints = []

  const tasktypes = [
    'TT_NORMAL','TT_PERIODIC','TT_OPENEND','TT_LOOP' ,'TT_TIEUP' ,'TT_MANUTASK','TT_VTRUPLOAD' ,'TT_OPENENDEX'
  ];
  
  if (!values.length) {
    return null
  }

  return (
    <table className="table">
      <tbody>
        <tr>
          <th style={{ textAlign: 'left' }}>Id</th>
          <th style={{ textAlign: 'right' }}>Type</th>
          <th style={{ textAlign: 'right' }}>Begin</th>
          <th style={{ textAlign: 'right' }}>End</th>
          <th style={{ textAlign: 'right' }}>State</th>
          <th style={{ textAlign: 'right' }}>SyncState</th>
          <th style={{ textAlign: 'right' }}>DispatchState</th>
        </tr>
        {values.map((item) =>{
         
            return (
              <tr key={item.taskid}>
                <td style={{ textOverflow: 'ellipsis' }} title={item.taskid}>
                </td>
                <td>
                  <span className="pull-right">
                    <strong>{item.tasktype}</strong>{tasktypes[item.tasktype]}
                  </span>
                </td>
                <td>
                  <span className="pull-right">
                    <strong>
                      {item.starttime}
                    </strong>
                  </span>
                </td>
                <td>
                  <span className="pull-right">
                    <strong>
                      {item.endtime}
                    </strong>{' '}
                  </span>
                </td>
                <td>
                  <span className="pull-right">
                    <strong>
                      {item.state}
                    </strong>{' '}
                  </span>
                </td>
                <td>
                  <span className="pull-right">
                    <strong>
                      {item.syncState}
                    </strong>{' '}
                  </span>
                </td>
                <td>
                  <span className="pull-right">
                    <strong>
                      {item.dispatchState}
                    </strong>{' '}
                  </span>
                </td>
              </tr>
            )
          
        })}
      </tbody>
    </table>
  )
}
// add multiple axis to the chart
// https://jsfiddle.net/devonuto/pa7k6xn9/
module.exports = class Grain extends React.Component {
  renderEmpty() {
    return <span>No messages recorded</span>
  }

  renderGraphs() {
    var stats = {
      activationCount: 0,
      totalSeconds: 0,
      totalAwaitTime: 0,
      totalCalls: 0,
      totalExceptions: 0
    }
    this.props.dashboardCounters.simpleGrainStats.forEach(stat => {
      if (stat.grainType !== this.props.grainType) return
      stats.activationCount += stat.activationCount
      stats.totalSeconds += stat.totalSeconds
      stats.totalAwaitTime += stat.totalAwaitTime
      stats.totalCalls += stat.totalCalls
      stats.totalExceptions += stat.totalExceptions
    })

    return (
      <Page
        title={getName(this.props.grainType)}
        subTitle={this.props.grainType}
      >
        <div>
          <div className="row">
            <div className="col-md-3">
              <CounterWidget
                icon="cubes"
                counter={stats.activationCount}
                title="Activations"
              />
            </div>
            <div className="col-md-3">
              <CounterWidget
                icon="bug"
                counter={
                  stats.totalCalls === 0
                    ? '0.00'
                    : (
                        (100 * stats.totalExceptions) /
                        stats.totalCalls
                      ).toFixed(2) + '%'
                }
                title="Error Rate"
              />
            </div>
            <div className="col-md-3">
              <CounterWidget
                icon="tachometer"
                counter={(stats.totalCalls / 100).toFixed(2)}
                title="Req/sec"
              />
            </div>
            <div className="col-md-3">
              <CounterWidget
                icon="clock-o"
                counter={
                  stats.totalCalls === 0
                    ? '0'
                    : (stats.totalAwaitTime / stats.totalCalls).toFixed(2) +
                      'ms'
                }
                title="Average response time"
              />
            </div>
          </div>
          {this.props.ingesttaskinfo != null? <Taskinfo stats={this.props.ingesttaskinfo}></Taskinfo>:null}
          <Panel title="Method Profiling">
            <div>
              <span>
                <strong style={{ color: '#783988', fontSize: '25px' }}>
                  /
                </strong>{' '}
                number of requests per second
                <br />
                <strong style={{ color: '#EC1F1F', fontSize: '25px' }}>
                  /
                </strong>{' '}
                failed requests
              </span>
              <span className="pull-right">
                <strong style={{ color: '#EC971F', fontSize: '25px' }}>
                  /
                </strong>{' '}
                average latency in milliseconds
              </span>
              {Object.keys(this.props.grainStats)
                .sort()
                .map(key => (
                  <GrainGraph key={key}
                    stats={this.props.grainStats[key]}
                    grainMethod={getName(key)}
                  />
                ))}
            </div>
          </Panel>

          <Panel title="Activations by Silo">
            <SiloBreakdown
              data={this.props.dashboardCounters.simpleGrainStats}
              grainType={this.props.grainType}
            />
          </Panel>
        </div>
      </Page>
    )
  }

  render() {
    if (Object.keys(this.props.grainStats).length === 0)
      return this.renderEmpty()
    return this.renderGraphs()
  }
}

function getName(value) {
  var parts = value.split('.')
  return parts[parts.length - 1]
}
